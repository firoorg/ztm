using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Ztm.Threading;
using Ztm.WebApi.Callbacks;
using Ztm.Zcoin.Synchronization;
using Ztm.Zcoin.Watching;
using Timer = Ztm.Threading.Timer;
using Watch = Ztm.Zcoin.Watching.TransactionWatch<Ztm.WebApi.Watchers.TransactionConfirmation.Rule>;

namespace Ztm.WebApi.Watchers.TransactionConfirmation
{
    public sealed class TransactionConfirmationWatcher : ITransactionConfirmationWatcher, IHostedService, IBlockListener, ITransactionConfirmationWatcherHandler<Rule>, IDisposable
    {
        readonly Ztm.Zcoin.Watching.TransactionConfirmationWatcher<Rule> watcher;

        // Providers
        readonly ICallbackRepository callbackRepository;
        readonly IRuleRepository ruleRepository;
        readonly IWatchRepository watchRepository;
        readonly ICallbackExecuter callbackExecuter;
        readonly ILogger<TransactionConfirmationWatcher> logger;

        // Dictionary from transaction to Dictionary from watch id to timer.
        readonly Dictionary<uint256, Dictionary<Guid, Timer>> timers;
        readonly ReaderWriterLockSlim timerLock;

        public TransactionConfirmationWatcher(
            ICallbackRepository callbackRepository,
            IRuleRepository ruleRepository,
            IBlocksStorage blocks,
            ICallbackExecuter callbackExecuter,
            IWatchRepository watchRepository,
            ILogger<TransactionConfirmationWatcher> logger)
        {
            if (callbackRepository == null)
            {
                throw new ArgumentNullException(nameof(callbackRepository));
            }

            if (ruleRepository == null)
            {
                throw new ArgumentNullException(nameof(ruleRepository));
            }

            if (blocks == null)
            {
                throw new ArgumentNullException(nameof(blocks));
            }

            if (callbackExecuter == null)
            {
                throw new ArgumentNullException(nameof(callbackExecuter));
            }

            if (watchRepository == null)
            {
                throw new ArgumentNullException(nameof(watchRepository));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            this.callbackRepository = callbackRepository;
            this.ruleRepository = ruleRepository;
            this.watchRepository = watchRepository;
            this.callbackExecuter = callbackExecuter;
            this.logger = logger;

            this.watcher = new Zcoin.Watching.TransactionConfirmationWatcher<Rule>
            (
                this,
                blocks
            );

            this.timers = new Dictionary<uint256, Dictionary<Guid, Timer>>();
            this.timerLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        }

        public async Task<Rule> AddTransactionAsync(
            uint256 transaction,
            int confirmation,
            TimeSpan unconfirmedWaitingTime,
            Callback callback,
            CallbackResult successResponse,
            CallbackResult timeoutResponse,
            CancellationToken cancellationToken)
        {
            if (transaction == null)
            {
                throw new ArgumentNullException(nameof(transaction));
            }

            if (confirmation < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(confirmation), "The confirmations number is less than zero.");
            }

            if (!Timer.DefaultScheduler.IsValidDuration(unconfirmedWaitingTime))
            {
                throw new ArgumentOutOfRangeException(nameof(unconfirmedWaitingTime), "UnconfirmedWaitingTime is invalid duration.");
            }

            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            if (successResponse == null)
            {
                throw new ArgumentNullException(nameof(successResponse));
            }

            if (timeoutResponse == null)
            {
                throw new ArgumentNullException(nameof(timeoutResponse));
            }

            var rule = await this.ruleRepository.AddAsync
            (
                transaction,
                confirmation,
                unconfirmedWaitingTime,
                successResponse,
                timeoutResponse,
                callback,
                cancellationToken
            );

            await SetupTimerAsync(rule);

            return rule;
        }

        public void Dispose()
        {
            this.timerLock.Dispose();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var rules = await this.ruleRepository.ListWaitingAsync(cancellationToken);

            foreach (var rule in rules)
            {
                await SetupTimerAsync(rule);
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            this.timerLock.EnterWriteLock();

            try
            {
                foreach (var timer in this.timers.SelectMany(ts => ts.Value))
                {
                    await timer.Value.StopAsync(cancellationToken);
                    if (timer.Value.ElapsedCount == 0)
                    {
                        await this.ruleRepository.SubtractRemainingWaitingTimeAsync(timer.Key, timer.Value.ElapsedTime, CancellationToken.None);
                    }
                }
            }
            finally
            {
                this.timerLock.ExitWriteLock();
            }
        }

        async Task SetupTimerAsync(Rule rule)
        {
            var timer = new Timer();

            this.timerLock.EnterWriteLock();
            try
            {
                Dictionary<Guid, Timer> timers;

                if (!this.timers.TryGetValue(rule.TransactionHash, out timers))
                {
                    timers = new Dictionary<Guid, Timer>();
                    this.timers.Add(rule.TransactionHash, timers);
                }

                timers.Add(rule.Id, timer);

                try
                {
                    var remainingWaitingTime = await this.ruleRepository.GetRemainingWaitingTimeAsync(rule.Id, CancellationToken.None);

                    timer.Elapsed += OnTimeout;
                    timer.Start(remainingWaitingTime < TimeSpan.Zero ? TimeSpan.Zero : remainingWaitingTime, null, rule);
                }
                catch
                {
                    timers.Remove(rule.Id);
                    throw;
                }
            }
            finally
            {
                this.timerLock.ExitWriteLock();
            }
        }

        void OnTimeout(object sender, TimerElapsedEventArgs e)
        {
            e.RegisterBackgroundTask(async cancellationToken =>
            {
                this.timerLock.EnterWriteLock();

                var rule = (Rule)e.Context;
                try
                {
                    await this.ruleRepository.UpdateStatusAsync(rule.Id, RuleStatus.Timeout, CancellationToken.None);
                    await ExecuteCallbackAsync(rule.Callback, rule.TimeoutResponse, CancellationToken.None);
                }
                catch (Exception ex) // lgtm [cs/catch-of-all-exceptions]
                {
                    this.logger.LogError(ex, "Timeout execution is fail.");
                }
                finally
                {
                    RemoveTimer(rule);
                    this.timerLock.ExitWriteLock();
                }
            });
        }

        void RemoveTimer(Rule rule)
        {
            this.timerLock.EnterWriteLock();

            try
            {
                if (!this.timers.TryGetValue(rule.TransactionHash, out var timers))
                {
                    throw new KeyNotFoundException("Transaction is not found.");
                }

                timers.Remove(rule.Id);

                if (timers.Count == 0)
                {
                    this.timers.Remove(rule.TransactionHash);
                }
            }
            finally
            {
                this.timerLock.ExitWriteLock();
            }
        }

        async Task<bool> StopTimer(Rule rule)
        {
            this.timerLock.EnterWriteLock();

            try
            {
                if (this.timers.TryGetValue(rule.TransactionHash, out var txTimers) && txTimers.TryGetValue(rule.Id, out var timer))
                {
                    await timer.StopAsync(CancellationToken.None);
                    if (timer.ElapsedCount == 0)
                    {
                        RemoveTimer(rule);
                        await this.ruleRepository.SubtractRemainingWaitingTimeAsync(rule.Id, timer.ElapsedTime, CancellationToken.None);

                        return true;
                    }
                }
            }
            finally
            {
                this.timerLock.ExitWriteLock();
            }

            return false;
        }

        async Task ConfirmAsync(Watch watch, CancellationToken cancellationToken)
        {
            await this.ruleRepository.UpdateStatusAsync(watch.Context.Id, RuleStatus.Success, cancellationToken);
            await ExecuteCallbackAsync(watch.Context.Callback, watch.Context.SuccessResponse, CancellationToken.None);
        }

        async Task ExecuteCallbackAsync(Callback callback, CallbackResult payload, CancellationToken cancellationToken)
        {
            await this.callbackRepository.AddHistoryAsync(callback.Id, payload, cancellationToken);

            try
            {
                await this.callbackExecuter.ExecuteAsync(callback.Id, callback.Url, payload, CancellationToken.None);
            }
            catch (Exception ex) // lgtm [cs/catch-of-all-exceptions]
            {
                this.logger.LogError($"Callback execution is fail.", ex);
                return;
            }

            await this.callbackRepository.SetCompletedAsyc(callback.Id, CancellationToken.None);
        }

        Task IBlockListener.BlockAddedAsync(Block block, int height, CancellationToken cancellationToken)
        {
            return this.watcher.ExecuteAsync(block, height, BlockEventType.Added, cancellationToken);
        }

        Task IBlockListener.BlockRemovingAsync(Block block, int height, CancellationToken cancellationToken)
        {
            return this.watcher.ExecuteAsync(block, height, BlockEventType.Removing, cancellationToken);
        }

        async Task<IEnumerable<Rule>> ITransactionConfirmationWatcherHandler<Rule>.CreateContextsAsync(Transaction tx, CancellationToken cancellationToken)
        {
            this.timerLock.EnterReadLock();

            try
            {
                if (this.timers.TryGetValue(tx.GetHash(), out var txTimers) && txTimers.Count > 0)
                {
                    var contexts = new Collection<Rule>();

                    foreach (var timer in txTimers)
                    {
                        var rule = await this.ruleRepository.GetAsync(timer.Key, cancellationToken);
                        contexts.Add(rule);
                    }

                    return contexts;
                }
            }
            finally
            {
                this.timerLock.ExitReadLock();
            }

            return Enumerable.Empty<Rule>();
        }

        async Task<bool> IConfirmationWatcherHandler<Rule, Watch, Watch>.ConfirmationUpdateAsync(
            Watch confirm,
            int count,
            ConfirmationType type,
            CancellationToken cancellationToken)
        {
            switch (type)
            {
                case ConfirmationType.Confirmed:
                    if (count >= confirm.Context.Confirmations)
                    {
                        await ConfirmAsync(confirm, cancellationToken);
                        return true;
                    }
                    break;
                case ConfirmationType.Unconfirming:
                    break;
                default:
                    throw new NotSupportedException($"{type} is not supported.");
            }

            return false;
        }

        Task<IEnumerable<Watch>> IConfirmationWatcherHandler<Rule, Watch, Watch>.GetCurrentWatchesAsync(
            CancellationToken cancellationToken)
        {
            return this.watchRepository.ListPendingAsync(null, cancellationToken);
        }

        async Task IWatcherHandler<Rule, Watch>.AddWatchesAsync(IEnumerable<Watch> watches, CancellationToken cancellationToken)
        {
            if (watches == null)
            {
                throw new ArgumentNullException(nameof(watches));
            }

            foreach (var watch in watches) // lgtm [cs/linq/missed-where]
            {
                if (await StopTimer(watch.Context))
                {
                    await this.watchRepository.AddAsync(watch, cancellationToken);
                    await this.ruleRepository.UpdateCurrentWatchAsync(watch.Context.Id, watch.Id, CancellationToken.None);
                }
            }
        }

        async Task IWatcherHandler<Rule, Watch>.RemoveUncompletedWatchesAsync(
            uint256 startedBlock,
            CancellationToken cancellationToken)
        {
            var watches = await this.watchRepository.ListPendingAsync(startedBlock, cancellationToken);

            foreach (var watch in watches)
            {
                await this.ruleRepository.UpdateCurrentWatchAsync(watch.Context.Id, null, CancellationToken.None);
                await this.watchRepository.SetRejectedAsync(watch.Id, CancellationToken.None);
                await SetupTimerAsync(watch.Context);
            }
        }

        async Task IWatcherHandler<Rule, Watch>.RemoveCompletedWatchesAsync(
            IEnumerable<Watch> watches,
            CancellationToken cancellationToken)
        {
            foreach (var watch in watches)
            {
                await this.ruleRepository.UpdateCurrentWatchAsync(watch.Context.Id, null, CancellationToken.None);
                await this.watchRepository.SetSucceededAsync(watch.Id, CancellationToken.None);
            }
        }
    }
}
