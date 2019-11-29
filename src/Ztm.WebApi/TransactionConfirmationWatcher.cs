using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Ztm.Threading;
using Ztm.Zcoin.Synchronization;
using Ztm.Zcoin.Watching;

using Rule = Ztm.WebApi.TransactionConfirmationWatchingRule<Ztm.WebApi.TransactionConfirmationCallbackResult>;
using Timer = Ztm.Threading.Timer;

namespace Ztm.WebApi
{
    public sealed class TransactionConfirmationWatcher : ITransactionConfirmationWatcher, IHostedService, IBlockListener, ITransactionConfirmationWatcherHandler<Rule>
    {
        readonly Ztm.Zcoin.Watching.TransactionConfirmationWatcher<Rule> watcher;

        // Providers
        readonly ICallbackRepository callbackRepository;
        readonly ITransactionConfirmationWatchingRuleRepository<TransactionConfirmationCallbackResult> ruleRepository;
        readonly ITransactionConfirmationWatchRepository watchRepository;
        readonly ICallbackExecuter callbackExecuter;
        readonly IBlocksStorage blocks;
        readonly ILogger<TransactionConfirmationWatcher> logger;

        // State recorders
        readonly ReaderWriterLockSlim timerLock;

        // Dictionary from transaction to Dictionary from watch id to timer and confirmations.
        readonly Dictionary<uint256, Dictionary<Guid, Timer>> timers;

        public TransactionConfirmationWatcher(
            ICallbackRepository callbackRepository,
            ITransactionConfirmationWatchingRuleRepository<TransactionConfirmationCallbackResult> ruleRepository,
            IBlocksStorage blocks,
            ICallbackExecuter callbackExecuter,
            ITransactionConfirmationWatchRepository watchRepository,
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
            this.blocks = blocks;
            this.logger = logger;

            this.watcher = new Zcoin.Watching.TransactionConfirmationWatcher<Rule>
            (
                this,
                blocks
            );

            this.timerLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
            this.timers = new Dictionary<uint256, Dictionary<Guid, Timer>>();
        }

        public async Task<Rule> AddTransactionAsync(
            uint256 transaction,
            int confirmation,
            TimeSpan unconfirmedWaitingTime,
            Callback callback,
            TransactionConfirmationCallbackResult successData,
            TransactionConfirmationCallbackResult timeoutData,
            CancellationToken cancellationToken)
        {
            if (transaction == null)
            {
                throw new ArgumentNullException(nameof(transaction));
            }

            if (confirmation < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(confirmation) ,"Confirmation is less than zero.");
            }

            if (!Timer.IsValidDuration(unconfirmedWaitingTime))
            {
                throw new ArgumentOutOfRangeException(nameof(unconfirmedWaitingTime), "UnconfirmedWaitingTime is invalid duration.");
            }

            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            if (successData == null)
            {
                throw new ArgumentNullException(nameof(successData));
            }

            if (timeoutData == null)
            {
                throw new ArgumentNullException(nameof(timeoutData));
            }

            var rule = await this.ruleRepository.AddAsync
            (
                transaction,
                confirmation,
                unconfirmedWaitingTime,
                successData,
                timeoutData,
                callback,
                cancellationToken
            );

            await SetupTimerAsync(rule);

            return rule;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var rules = await this.ruleRepository.ListAsync(cancellationToken);

            var pendingWatches = await this.watchRepository.ListAsync(TransactionConfirmationWatchingWatchStatus.Pending, cancellationToken);

            foreach (var rule in rules.Where(r => !r.Completed && !pendingWatches.Any(w => w.Context.Id == r.Id)))
            {
                await SetupTimerAsync(rule);
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            this.timerLock.EnterWriteLock();

            try
            {
                foreach (var timerSet in timers)
                {
                    foreach (var timer in timerSet.Value)
                    {
                        await timer.Value.StopAsync(cancellationToken);
                        if (timer.Value.ElapsedCount == 0)
                        {
                            await this.ruleRepository.SubtractRemainingWaitingTimeAsync(timer.Key, timer.Value.ElapsedTime, CancellationToken.None);
                        }
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

                if (!this.timers.TryGetValue(rule.Transaction, out timers))
                {
                    timers = new Dictionary<Guid, Timer>();
                    this.timers.Add(rule.Transaction, timers);
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

                try
                {
                    var rule = (Rule)e.Context;

                    await this.ruleRepository.CompleteAsync(rule.Id, CancellationToken.None);
                    await ExecuteCallbackAsync(rule.Callback, rule.Timeout, cancellationToken);
                    RemoveTimer(rule);
                }
                finally
                {
                    this.timerLock.ExitWriteLock();
                }
            });
        }

        void RemoveTimer(Rule rule)
        {
            this.timerLock.EnterWriteLock();

            try
            {
                if (!this.timers.TryGetValue(rule.Transaction, out var timers))
                {
                    throw new KeyNotFoundException("Transaction is not found");
                }

                timers.Remove(rule.Id);

                if (timers.Count == 0)
                {
                    this.timers.Remove(rule.Transaction);
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
                if (this.timers.TryGetValue(rule.Transaction, out var txTimers) && txTimers.TryGetValue(rule.Id, out var timer))
                {
                    await timer.StopAsync(CancellationToken.None);
                    if (timer.ElapsedCount == 0)
                    {
                        await this.ruleRepository.SubtractRemainingWaitingTimeAsync(rule.Id, timer.ElapsedTime, CancellationToken.None);
                        RemoveTimer(rule);

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

        async Task ConfirmAsync(TransactionWatch<Rule> watch, CancellationToken cancellationToken)
        {
            var rule = await this.ruleRepository.GetAsync(watch.Context.Id, cancellationToken);

            await this.ruleRepository.CompleteAsync(watch.Context.Id, cancellationToken);
            await ExecuteCallbackAsync(rule.Callback, rule.Success, CancellationToken.None);
        }

        async Task ExecuteCallbackAsync(Callback callback, TransactionConfirmationCallbackResult payload, CancellationToken cancellationToken)
        {
            var id = await this.callbackRepository.AddHistoryAsync(callback.Id, payload, cancellationToken);

            try
            {
                await this.callbackExecuter.Execute(callback.Id, callback.Url, payload);
            }
            catch (HttpRequestException ex) // lgtm[cs/empty-catch-block]
            {
                this.logger.LogError($"Fail to execute callback, {ex}");
                return;
            }

            await this.callbackRepository.SetHistorySuccessAsync(id, CancellationToken.None);
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

        async Task<bool> IConfirmationWatcherHandler<TransactionWatch<Rule>, Rule>.ConfirmationUpdateAsync(TransactionWatch<Rule> watch, int confirmation, ConfirmationType type, CancellationToken cancellationToken)
        {
            switch (type)
            {
            case ConfirmationType.Unconfirming:
                if (confirmation == 1)
                {
                    return false;
                }
                break;

            case ConfirmationType.Confirmed:
                if (confirmation == 1)
                {
                    return false;
                }

                var requiredConfirmations = watch.Context.Confirmation;

                if (confirmation >= requiredConfirmations)
                {
                    await ConfirmAsync(watch, cancellationToken);
                    return true;
                }

                break;

            default:
                throw new NotSupportedException($"{nameof(ConfirmationType)} is not supported");
            }

            return false;
        }

        Task<IEnumerable<TransactionWatch<Rule>>> IConfirmationWatcherHandler<TransactionWatch<Rule>, Rule>.GetCurrentWatchesAsync(CancellationToken cancellationToken)
        {
            return this.watchRepository.ListAsync(TransactionConfirmationWatchingWatchStatus.Pending, cancellationToken);
        }

        async Task IWatcherHandler<TransactionWatch<Rule>, Rule>.AddWatchesAsync(IEnumerable<TransactionWatch<Rule>> watches, CancellationToken cancellationToken)
        {
            if (watches == null)
            {
                throw new ArgumentNullException(nameof(watches));
            }

            foreach (var watch in watches)
            {
                if (await StopTimer(watch.Context))
                {
                    await this.watchRepository.AddAsync(watch, cancellationToken);
                }
            }
        }

        async Task IWatcherHandler<TransactionWatch<Rule>, Rule>.RemoveWatchAsync(TransactionWatch<Rule> watch, WatchRemoveReason reason, CancellationToken cancellationToken)
        {
            await this.watchRepository.UpdateStatusAsync(watch.Id, TransactionConfirmationWatchingWatchStatus.Rejected, cancellationToken);

            if (reason == WatchRemoveReason.BlockRemoved)
            {
                await SetupTimerAsync(watch.Context);
            }
        }
    }
}
