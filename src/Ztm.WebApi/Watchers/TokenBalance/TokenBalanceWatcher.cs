using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Ztm.Threading;
using Ztm.WebApi.Callbacks;
using Ztm.Zcoin.NBitcoin.Exodus;
using Ztm.Zcoin.NBitcoin.Exodus.TransactionRetrievers;
using Ztm.Zcoin.Synchronization;
using Ztm.Zcoin.Watching;
using Change = Ztm.Zcoin.Watching.BalanceChange<Ztm.WebApi.Watchers.TokenBalance.Rule, Ztm.Zcoin.NBitcoin.Exodus.PropertyAmount>;
using Confirmation = Ztm.Zcoin.Watching.BalanceConfirmation<Ztm.WebApi.Watchers.TokenBalance.Rule, Ztm.Zcoin.NBitcoin.Exodus.PropertyAmount>;
using Timer = Ztm.Threading.Timer;
using Watch = Ztm.Zcoin.Watching.BalanceWatch<Ztm.WebApi.Watchers.TokenBalance.Rule, Ztm.Zcoin.NBitcoin.Exodus.PropertyAmount>;

namespace Ztm.WebApi.Watchers.TokenBalance
{
    public sealed class TokenBalanceWatcher :
        IBalanceWatcherHandler<Rule, PropertyAmount>,
        IBlockListener,
        IDisposable,
        IHostedService,
        ITokenBalanceWatcher
    {
        readonly PropertyId property;
        readonly ILogger logger;
        readonly ITransactionRetriever exodusRetriever;
        readonly IRuleRepository rules;
        readonly IWatchRepository watches;
        readonly ICallbackRepository callbacks;
        readonly ICallbackExecuter callbackExecutor;
        readonly ITimerScheduler timerScheduler;
        readonly Dictionary<BitcoinAddress, WatchingInfo> watching;
        readonly BalanceWatcher<Rule, PropertyAmount> engine;
        readonly SemaphoreSlim semaphore;
        bool disposed, stopped;

        public TokenBalanceWatcher(
            PropertyId property,
            ILogger<TokenBalanceWatcher> logger,
            IBlocksStorage blocks,
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
            this.exodusRetriever = exodusRetriever;
            this.rules = rules;
            this.watches = watches;
            this.callbacks = callbacks;
            this.callbackExecutor = callbackExecutor;
            this.timerScheduler = timerScheduler;
            this.watching = new Dictionary<BitcoinAddress, WatchingInfo>();
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

        public async Task<Rule> StartWatchAsync(
            BitcoinAddress address,
            PropertyAmount targetAmount,
            int targetConfirmation,
            TimeSpan timeout,
            string timeoutStatus,
            Guid callback,
            CancellationToken cancellationToken)
        {
            Rule rule;

            if (!this.timerScheduler.IsValidDuration(timeout))
            {
                throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "The value is not valid.");
            }

            if ((await this.callbacks.GetAsync(callback, cancellationToken)) == null)
            {
                throw new ArgumentException("The value is not a valid callback identifier.", nameof(callback));
            }

            await this.semaphore.WaitAsync(cancellationToken);

            try
            {
                if (this.stopped)
                {
                    throw new InvalidOperationException("The watcher is already stopped.");
                }

                rule = new Rule(
                    this.property,
                    address,
                    targetAmount,
                    targetConfirmation,
                    timeout,
                    timeoutStatus,
                    callback);

                await this.rules.AddAsync(rule, cancellationToken);

                StartTimer(rule, timeout);
            }
            finally
            {
                this.semaphore.Release();
            }

            return rule;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            IEnumerable<WatchingInfo> watchings;

            // Cache watching list due to we can't wait timer to stop while we hold the semaphore.
            await this.semaphore.WaitAsync(cancellationToken);

            try
            {
                watchings = this.watching
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

            this.watching.Add(rule.Address, new WatchingInfo(rule, timer));

            try
            {
                timer.Elapsed += (sender, e) =>
                {
                    e.RegisterBackgroundTask(
                        cancellationToken => OnTimeoutAsync((BitcoinAddress)e.Context, cancellationToken));
                };

                timer.Start(timeout, null, rule.Address);
            }
            catch
            {
                this.watching.Remove(rule.Address);
                throw;
            }
        }

        async Task OnTimeoutAsync(BitcoinAddress address, CancellationToken cancellationToken)
        {
            await this.semaphore.WaitAsync();

            try
            {
                if (!this.watching.Remove(address, out var watching))
                {
                    // Already completed.
                    return;
                }

                var rule = watching.Rule;

                await this.rules.SetTimedOutAsync(rule.Id, CancellationToken.None);

                // Mark all watches that was created by this rule as timed out and calculate total received amount.
                var watches = await this.watches.TransitionToTimedOutAsync(rule, CancellationToken.None);
                var received = default(PropertyAmount?);
                var confirmation = default(int?);

                foreach (var completed in watches.Where(w => w.Confirmation > 0))
                {
                    received = (received == null)
                        ? completed.Watch.BalanceChange
                        : received + completed.Watch.BalanceChange;

                    if (confirmation == null || completed.Confirmation < confirmation)
                    {
                        confirmation = completed.Confirmation;
                    }
                }

                // Invoke callback.
                var result = new TimeoutData()
                {
                    Received = received,
                    Confirmation = confirmation,
                    TargetConfirmation = rule.TargetConfirmation,
                };

                await InvokeCallbackAsync(rule, rule.TimeoutStatus, result, CancellationToken.None);
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

        async Task InvokeCallbackAsync(Rule rule, string status, object data, CancellationToken cancellationToken)
        {
            var callback = await this.callbacks.GetAsync(rule.Callback, cancellationToken);

            if (callback == null)
            {
                this.logger.LogError("No callback associated with rule {Rule}", rule.Id);
                return;
            }

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
                    watches.Where(w => this.watching.ContainsKey(w.Address)),
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
            WatchingInfo watching;

            await this.semaphore.WaitAsync(cancellationToken);

            try
            {
                if (!this.watching.ContainsKey(rule.Address))
                {
                    // Already timed out.
                    return false;
                }

                await this.watches.SetConfirmationCountAsync(confirm.Watches, cancellationToken);

                // Check if completed.
                if (type == ConfirmationType.Unconfirming || count < rule.TargetConfirmation)
                {
                    return false;
                }

                var received = confirm.Watches.Aggregate(
                    PropertyAmount.Zero,
                    (sum, next) => sum + next.Key.BalanceChange);

                if (received < rule.TargetAmount)
                {
                    return false;
                }

                // Trigger completion.
                this.watching.Remove(rule.Address, out watching);

                await this.rules.SetSucceededAsync(rule.Id, CancellationToken.None);

                // Invoke callback.
                var result = new CallbackData()
                {
                    Received = received,
                    Confirmation = count,
                };

                await InvokeCallbackAsync(rule, CallbackResult.StatusSuccess, result, CancellationToken.None);
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
            var groups = changes.Where(c => c.Property == this.property).GroupBy(
                c => c.Address,
                c => c.Amount,
                (k, v) => new { Address = k, Sum = v.Aggregate((current, next) => current + next) });

            // Check if address that have balance change is in our watching list.
            await this.semaphore.WaitAsync(cancellationToken);

            try
            {
                foreach (var group in groups)
                {
                    // Don't skip zero sum here until we are really sure it does not cause any vulnerabilities. The only
                    // behavior introduced by zero change is resetting confirmation count, which is safe than skipping
                    // zero change without knowing it vulnerabilities.
                    var address = group.Address;

                    if (!this.watching.TryGetValue(address, out var watching))
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
                // We wrapped this with semaphore so it is easy to understand with the statements in time out handler.
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
                // We wrapped this with semaphore so it is easy to synchronized our head with the statements in the time
                // out handler.
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
