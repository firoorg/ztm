using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Ztm.Threading;
using Ztm.WebApi.AddressPools;
using Ztm.WebApi.Callbacks;
using Ztm.Zcoin.NBitcoin.Exodus;
using Ztm.Zcoin.NBitcoin.Exodus.TransactionRetrievers;
using Ztm.Zcoin.Synchronization;
using Ztm.Zcoin.Watching;
using Change = Ztm.Zcoin.Watching.BalanceChange<Ztm.WebApi.Watchers.TokenReceiving.Rule, Ztm.Zcoin.NBitcoin.Exodus.PropertyAmount>;
using Confirmation = Ztm.Zcoin.Watching.BalanceConfirmation<Ztm.WebApi.Watchers.TokenReceiving.Rule, Ztm.Zcoin.NBitcoin.Exodus.PropertyAmount>;
using Timer = Ztm.Threading.Timer;
using Watch = Ztm.Zcoin.Watching.BalanceWatch<Ztm.WebApi.Watchers.TokenReceiving.Rule, Ztm.Zcoin.NBitcoin.Exodus.PropertyAmount>;

namespace Ztm.WebApi.Watchers.TokenReceiving
{
    public sealed class TokenReceivingWatcher :
        IBalanceWatcherHandler<Rule, PropertyAmount>,
        IBlockListener,
        IDisposable,
        IHostedService,
        ITokenReceivingWatcher
    {
        readonly PropertyId property;
        readonly ILogger logger;
        readonly IReceivingAddressPool addressPool;
        readonly ITransactionRetriever exodusRetriever;
        readonly IRuleRepository rules;
        readonly IWatchRepository watches;
        readonly ICallbackRepository callbacks;
        readonly ICallbackExecuter callbackExecutor;
        readonly ITimerScheduler timerScheduler;
        readonly Dictionary<BitcoinAddress, Watching> watchings;
        readonly BalanceWatcher<Rule, PropertyAmount> engine;
        readonly SemaphoreSlim semaphore;
        bool disposed, stopped;

        public TokenReceivingWatcher(
            PropertyId property,
            ILogger<TokenReceivingWatcher> logger,
            IBlocksStorage blocks,
            IReceivingAddressPool addressPool,
            ITransactionRetriever exodusRetriever,
            IRuleRepository rules,
            IWatchRepository watches,
            ICallbackRepository callbacks,
            ICallbackExecuter callbackExecutor,
            ITimerScheduler timerScheduler)
        {
            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (addressPool == null)
            {
                throw new ArgumentNullException(nameof(addressPool));
            }

            if (exodusRetriever == null)
            {
                throw new ArgumentNullException(nameof(exodusRetriever));
            }

            if (rules == null)
            {
                throw new ArgumentNullException(nameof(rules));
            }

            if (watches == null)
            {
                throw new ArgumentNullException(nameof(watches));
            }

            if (callbacks == null)
            {
                throw new ArgumentNullException(nameof(callbacks));
            }

            if (callbackExecutor == null)
            {
                throw new ArgumentNullException(nameof(callbackExecutor));
            }

            if (timerScheduler == null)
            {
                throw new ArgumentNullException(nameof(timerScheduler));
            }

            this.property = property;
            this.logger = logger;
            this.addressPool = addressPool;
            this.exodusRetriever = exodusRetriever;
            this.rules = rules;
            this.watches = watches;
            this.callbacks = callbacks;
            this.callbackExecutor = callbackExecutor;
            this.timerScheduler = timerScheduler;
            this.watchings = new Dictionary<BitcoinAddress, Watching>();
            this.engine = new BalanceWatcher<Rule, PropertyAmount>(this, blocks);
            this.semaphore = new SemaphoreSlim(1, 1);
            this.stopped = true;
        }

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.semaphore.Dispose();

            this.disposed = true;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await this.semaphore.WaitAsync(cancellationToken);

            try
            {
                var uncompleted = await this.rules.ListUncompletedAsync(this.property, cancellationToken);

                foreach (var rule in uncompleted)
                {
                    var timeout = await this.rules.GetCurrentTimeoutAsync(rule.Id, CancellationToken.None);

                    StartTimer(rule, timeout);
                }

                this.stopped = false;
            }
            finally
            {
                this.semaphore.Release();
            }
        }

        public async Task<Guid> StartWatchAsync(
            ReceivingAddressReservation address,
            PropertyAmount targetAmount,
            int targetConfirmation,
            TimeSpan timeout,
            TokenReceivingCallback callback,
            CancellationToken cancellationToken)
        {
            Rule rule;

            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            if (address.ReleasedDate != null)
            {
                throw new ArgumentException("The reservation is already released.", nameof(address));
            }

            if (!this.timerScheduler.IsValidDuration(timeout))
            {
                throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "The value is not valid.");
            }

            if (callback != null && callback.Completed)
            {
                throw new ArgumentException("The callback is already completed.", nameof(callback));
            }

            await this.semaphore.WaitAsync(cancellationToken);

            try
            {
                if (this.stopped)
                {
                    throw new InvalidOperationException("The watcher is already stopped.");
                }

                rule = new Rule(this.property, address, targetAmount, targetConfirmation, timeout, callback);

                await this.rules.AddAsync(rule, cancellationToken);

                StartTimer(rule, timeout);
            }
            finally
            {
                this.semaphore.Release();
            }

            return rule.Id;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            IEnumerable<Watching> watchings;

            // Cache watching list due to we can't wait timer to stop while we hold the semaphore.
            await this.semaphore.WaitAsync(cancellationToken);

            try
            {
                watchings = this.watchings
                    .Select(p => p.Value)
                    .ToList();

                this.stopped = true;
            }
            finally
            {
                this.semaphore.Release();
            }

            // Stop all timers.
            foreach (var (rule, timer) in watchings)
            {
                await timer.StopAsync(CancellationToken.None);

                if (timer.ElapsedCount != 0)
                {
                    continue;
                }

                await this.rules.DecreaseTimeoutAsync(rule.Id, timer.ElapsedTime, CancellationToken.None);
            }
        }

        void StartTimer(Rule rule, TimeSpan timeout)
        {
            if (this.semaphore.CurrentCount > 0)
            {
                throw new InvalidOperationException("The semaphore must be held before a timer can start.");
            }

            var timer = new Timer(this.timerScheduler);
            var address = rule.AddressReservation.Address.Address;

            this.watchings.Add(address, new Watching(rule, timer));

            try
            {
                timer.Elapsed += (sender, e) =>
                {
                    e.RegisterBackgroundTask(
                        cancellationToken => OnTimeoutAsync((BitcoinAddress)e.Context, cancellationToken));
                };

                timer.Start(timeout, null, address);
            }
            catch
            {
                this.watchings.Remove(address);
                throw;
            }
        }

        async Task OnTimeoutAsync(BitcoinAddress address, CancellationToken cancellationToken)
        {
            await this.semaphore.WaitAsync();

            try
            {
                if (!this.watchings.Remove(address, out var watching))
                {
                    // Already completed.
                    return;
                }

                var rule = watching.Rule;

                await this.rules.SetTimedOutAsync(rule.Id, CancellationToken.None);
                await this.addressPool.ReleaseAddressAsync(rule.AddressReservation.Id, CancellationToken.None);

                // Mark all watches that was created by this rule as timed out and calculate total received amount.
                var watches = await this.watches.TransitionToTimedOutAsync(rule, CancellationToken.None);
                var callback = rule.Callback;

                // Invoke callback.
                if (callback != null)
                {
                    var confirmed = watches.Where(p => p.Value > 0).ToDictionary(p => p.Key, p => p.Value);
                    var amount = SumChanges(confirmed, rule.TargetConfirmation);

                    var result = new CallbackData()
                    {
                        Received = amount,
                    };

                    await InvokeCallbackAsync(
                        callback.Callback,
                        callback.TimeoutStatus,
                        result,
                        CancellationToken.None);
                }
            }
            catch (Exception ex) // lgtm[cs/catch-of-all-exceptions]
            {
                this.logger.LogError(ex, "Failed to trigger timeout for address {Address}", address);
            }
            finally
            {
                this.semaphore.Release();
            }
        }

        CallbackAmount SumChanges(IReadOnlyDictionary<Watch, int> changes, int requiredConfirmation)
        {
            var amount = new CallbackAmount();

            foreach (var p in changes)
            {
                if (p.Value >= requiredConfirmation)
                {
                    amount.Confirmed = (amount.Confirmed ?? PropertyAmount.Zero) + p.Key.BalanceChange;
                }
                else
                {
                    amount.Pending = (amount.Pending ?? PropertyAmount.Zero) + p.Key.BalanceChange;
                }
            }

            return amount;
        }

        async Task InvokeCallbackAsync(
            Callback callback,
            string status,
            object data,
            CancellationToken cancellationToken)
        {
            var result = new CallbackResult(status, data);

            await this.callbacks.AddHistoryAsync(callback.Id, result, cancellationToken);
            await this.callbackExecutor.ExecuteAsync(callback.Id, callback.Url, result, CancellationToken.None);
            await this.callbacks.SetCompletedAsyc(callback.Id, CancellationToken.None);
        }

        async Task IWatcherHandler<Rule, Watch>.AddWatchesAsync(
            IEnumerable<Watch> watches,
            CancellationToken cancellationToken)
        {
            await this.semaphore.WaitAsync(cancellationToken);

            try
            {
                await this.watches.AddAsync(
                    watches.Where(w => this.watchings.ContainsKey(w.Address)), // Ensure it not timeout yet.
                    cancellationToken);
            }
            finally
            {
                this.semaphore.Release();
            }
        }

        async Task<bool> IConfirmationWatcherHandler<Rule, Watch, Confirmation>.ConfirmationUpdateAsync(
            Confirmation confirm,
            int count,
            ConfirmationType type,
            CancellationToken cancellationToken)
        {
            // All watches MUST come from the same rule; otherwise that mean there is a bug somewhere.
            var rule = confirm.Watches.Select(w => w.Key.Context).Distinct().Single();
            var address = rule.AddressReservation.Address.Address;
            Watching watching;

            await this.semaphore.WaitAsync(cancellationToken);

            try
            {
                if (!this.watchings.ContainsKey(address))
                {
                    // Already timed out.
                    return false;
                }

                // Update confirmation count for all watches.
                switch (type)
                {
                    case ConfirmationType.Confirmed:
                        await this.watches.SetConfirmationCountAsync(confirm.Watches, cancellationToken);
                        break;
                    case ConfirmationType.Unconfirming:
                        await this.watches.SetConfirmationCountAsync(
                            confirm.Watches.ToDictionary(
                                p => p.Key,
                                p =>
                                {
                                    var confirmation = p.Value - 1;

                                    if (confirmation < 0)
                                    {
                                        throw new ArgumentException(
                                            "Some watches contains an invalid confirmation count.",
                                            nameof(confirm));
                                    }

                                    return confirmation;
                                }),
                            cancellationToken);
                        return false;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(type), type, "The value is unsupported.");
                }

                // Check if completed.
                var amount = SumChanges(confirm.Watches, rule.TargetConfirmation);

                if (amount.Confirmed == null || amount.Confirmed < rule.TargetAmount)
                {
                    return false;
                }

                // Trigger completion.
                this.watchings.Remove(address, out watching);

                await this.rules.SetSucceededAsync(rule.Id, CancellationToken.None);
                await this.addressPool.ReleaseAddressAsync(rule.AddressReservation.Id, CancellationToken.None);

                // Invoke callback.
                var callback = rule.Callback;

                if (callback != null)
                {
                    var result = new CallbackData()
                    {
                        Received = amount,
                    };

                    await InvokeCallbackAsync(
                        callback.Callback,
                        CallbackResult.StatusSuccess,
                        result,
                        CancellationToken.None);
                }
            }
            finally
            {
                this.semaphore.Release();
            }

            // Stop timer. We need to do this outside semaphore otherwise it will cause dead lock.
            await watching.Timer.StopAsync(CancellationToken.None);

            return true;
        }

        async Task<IReadOnlyDictionary<BitcoinAddress, Change>> IBalanceWatcherHandler<Rule, PropertyAmount>.GetBalanceChangesAsync(
            Transaction tx,
            CancellationToken cancellationToken)
        {
            // Get all balance changes in the transaction.
            var changes = await this.exodusRetriever.GetBalanceChangesAsync(tx, cancellationToken);
            var result = new Dictionary<BitcoinAddress, Change>();

            if (changes == null)
            {
                // Not an Exodus transaction.
                return result;
            }

            // Filter out change that does not belong to the target property and sum all changes for each address.
            var groups = changes
                .Where(c => c.Property == this.property && c.Amount > PropertyAmount.Zero)
                .GroupBy(
                    c => c.Address,
                    c => c.Amount,
                    (k, v) => new { Address = k, Sum = v.Aggregate((sum, next) => sum + next) });

            // Check if address that have balance change is in our watching list.
            await this.semaphore.WaitAsync(cancellationToken);

            try
            {
                foreach (var group in groups)
                {
                    var address = group.Address;

                    if (!this.watchings.TryGetValue(address, out var watching))
                    {
                        continue;
                    }

                    result.Add(address, new Change(watching.Rule, group.Sum));
                }
            }
            finally
            {
                this.semaphore.Release();
            }

            return result;
        }

        async Task<IEnumerable<Watch>> IConfirmationWatcherHandler<Rule, Watch, Confirmation>.GetCurrentWatchesAsync(
            CancellationToken cancellationToken)
        {
            await this.semaphore.WaitAsync(cancellationToken);

            try
            {
                // We wrapped this with semaphore so it is easy to understand with the statements in timeout handler.
                return await this.watches.ListUncompletedAsync(this.property, cancellationToken);
            }
            finally
            {
                this.semaphore.Release();
            }
        }

        Task IWatcherHandler<Rule, Watch>.RemoveCompletedWatchesAsync(
            IEnumerable<Watch> watches,
            CancellationToken cancellationToken)
        {
            // Once we reached here we don't need to lock anymore since the addresses already removed from watching
            // list.
            return this.watches.TransitionToSucceededAsync(watches, cancellationToken);
        }

        async Task IWatcherHandler<Rule, Watch>.RemoveUncompletedWatchesAsync(
            uint256 startedBlock,
            CancellationToken cancellationToken)
        {
            await this.semaphore.WaitAsync(cancellationToken);

            try
            {
                // We wrapped this with semaphore so it is easy to synchronized our head with the statements in the
                // timeout handler.
                await this.watches.TransitionToRejectedAsync(this.property, startedBlock, cancellationToken);
            }
            finally
            {
                this.semaphore.Release();
            }
        }

        Task IBlockListener.BlockAddedAsync(Block block, int height, CancellationToken cancellationToken)
        {
            return this.engine.ExecuteAsync(block, height, BlockEventType.Added, cancellationToken);
        }

        Task IBlockListener.BlockRemovingAsync(Block block, int height, CancellationToken cancellationToken)
        {
            return this.engine.ExecuteAsync(block, height, BlockEventType.Removing, cancellationToken);
        }
    }
}
