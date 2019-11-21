using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using NBitcoin;
using Ztm.Threading;
using Ztm.Zcoin.Synchronization;
using Ztm.Zcoin.Watching;

using ConfirmContext = Ztm.WebApi.TransactionConfirmationWatch<Ztm.WebApi.TransactionConfirmationCallbackResult>;
using Timer = Ztm.Threading.Timer;

namespace Ztm.WebApi
{
    public sealed class TransactionConfirmationWatcher : ITransactionConfirmationWatcher, IHostedService, IBlockListener, ITransactionConfirmationWatcherHandler<ConfirmContext>
    {
        readonly Ztm.Zcoin.Watching.TransactionConfirmationWatcher<ConfirmContext> watcher;

        // Providers
        readonly ICallbackRepository callbackRepository;
        readonly ITransactionConfirmationWatchRepository<TransactionConfirmationCallbackResult> watchRepository;
        readonly ICallbackExecuter callbackExecuter;

        // State recorders
        readonly ReaderWriterLockSlim timerLock;

        // Dictionary from transaction to Dictionary from watch id to timer and confirmations.
        readonly Dictionary<uint256, Dictionary<Guid, Timer>> timers;
        readonly ConcurrentDictionary<Guid, TransactionWatch<ConfirmContext>> watches;

        public TransactionConfirmationWatcher(
            ICallbackRepository callbackRepository,
            ITransactionConfirmationWatchRepository<TransactionConfirmationCallbackResult> watchRepository,
            IBlocksStorage blocks,
            ICallbackExecuter callbackExecuter)
        {
            if (callbackRepository == null)
            {
                throw new ArgumentNullException(nameof(callbackRepository));
            }

            if (watchRepository == null)
            {
                throw new ArgumentNullException(nameof(watchRepository));
            }

            if (blocks == null)
            {
                throw new ArgumentNullException(nameof(blocks));
            }

            if (callbackExecuter == null)
            {
                throw new ArgumentNullException(nameof(callbackExecuter));
            }

            this.callbackRepository = callbackRepository;
            this.watchRepository = watchRepository;
            this.callbackExecuter = callbackExecuter;

            this.watcher = new Zcoin.Watching.TransactionConfirmationWatcher<ConfirmContext>
            (
                this,
                blocks
            );

            this.timerLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
            timers = new Dictionary<uint256, Dictionary<Guid, Timer>>();
            watches = new ConcurrentDictionary<Guid, TransactionWatch<ConfirmContext>>();
        }

        public async Task<ConfirmContext> AddTransactionAsync(
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

            var watch = await this.watchRepository.AddAsync
            (
                transaction,
                confirmation,
                unconfirmedWaitingTime,
                successData,
                timeoutData,
                callback,
                cancellationToken
            );

            await SetupTimerAsync(watch.Id);

            return watch;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var allWatches = await this.watchRepository.ListAsync(cancellationToken);

            foreach (var watch in allWatches.Where(w => !w.Completed))
            {
                await SetupTimerAsync(watch.Id);
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            this.timerLock.EnterWriteLock();

            try
            {
                foreach (var timerSet in timers)
                {
                    foreach (var timer in timerSet.Value.Where(t => t.Value.Status == TimerStatus.Started))
                    {
                        await timer.Value.StopAsync(cancellationToken);
                    }
                }
            }
            finally
            {
                this.timerLock.ExitWriteLock();
            }
        }

        async Task SetupTimerAsync(Guid id)
        {
            var timer = new Timer();

            this.timerLock.EnterWriteLock();

            try
            {
                var watch = await this.watchRepository.GetAsync(id, CancellationToken.None);

                Dictionary<Guid, Timer> timers;

                if (!this.timers.TryGetValue(watch.Transaction, out timers))
                {
                    timers = new Dictionary<Guid, Timer>();
                    this.timers.Add(watch.Transaction, timers);
                }

                timers.Add(watch.Id, timer);

                try
                {
                    timer.Elapsed += OnTimeout;
                    timer.Start(watch.RemainingWaitingTime < TimeSpan.Zero ? TimeSpan.Zero : watch.RemainingWaitingTime, null, watch.Id);
                }
                catch
                {
                    timers.Remove(watch.Id);
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
                var watch = await watchRepository.GetAsync((Guid)e.Context, cancellationToken);

                await this.watchRepository.CompleteAsync((Guid)e.Context, cancellationToken);
                await ExecuteCallbackAsync(watch.Callback, watch.Timeout, cancellationToken);
                RemoveTimer(watch.Transaction, watch.Id);
            });
        }

        void RemoveTimer(uint256 transaction, Guid id)
        {
            this.timerLock.EnterWriteLock();

            try
            {
                if (!this.timers.TryGetValue(transaction, out var timers))
                {
                    throw new KeyNotFoundException("Transaction is not found");
                }

                timers.Remove(id);

                if (timers.Count == 0)
                {
                    this.timers.Remove(transaction);
                }
            }
            finally
            {
                this.timerLock.ExitWriteLock();
            }
        }

        async Task<bool> StopTimer(uint256 transaction, Guid id)
        {
            this.timerLock.EnterWriteLock();
            try
            {
                if (this.timers.TryGetValue(transaction, out var txTimers) && txTimers.TryGetValue(id, out var timer))
                {
                    await timer.StopAsync(CancellationToken.None);
                    if (timer.ElapsedCount == 0)
                    {
                        var watchData = await this.watchRepository.GetAsync(id, CancellationToken.None);
                        await this.watchRepository.SetRemainingWaitingTimeAsync(id, watchData.RemainingWaitingTime - timer.ElapsedTime, CancellationToken.None);

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

        async Task UpdateRemainingWaitingTimeAsync(Guid id, TimeSpan remainingWaitingTime, CancellationToken cancellationToken)
        {
            await this.watchRepository.SetRemainingWaitingTimeAsync(id, remainingWaitingTime, cancellationToken);
        }

        async Task ResumeTimerAsync(Guid id)
        {
            this.timerLock.EnterWriteLock();

            try
            {
                var watch = await watchRepository.GetAsync(id, CancellationToken.None);
                RemoveTimer(watch.Transaction, id);

                await SetupTimerAsync(id);
            }
            finally
            {
                this.timerLock.ExitWriteLock();
            }
        }

        async Task ConfirmAsync(TransactionWatch<ConfirmContext> watch, CancellationToken cancellationToken)
        {
            var watchObject = await this.watchRepository.GetAsync(watch.Context.Id, cancellationToken);

            await this.watchRepository.CompleteAsync(watch.Context.Id, cancellationToken);
            await ExecuteCallbackAsync(watchObject.Callback, watchObject.Success, cancellationToken);
        }

        async Task ExecuteCallbackAsync(Callback callback, TransactionConfirmationCallbackResult payload, CancellationToken cancellationToken)
        {
            var id = await this.callbackRepository.AddHistoryAsync(callback.Id, payload, cancellationToken);

            try
            {
                await this.callbackExecuter.Execute(callback.Id, callback.Url, payload);
                await this.callbackRepository.SetHistorySuccessAsync(id, cancellationToken);
                await this.callbackRepository.SetCompletedAsyc(callback.Id, CancellationToken.None);
            }
            catch (HttpRequestException) // lgtm[cs/empty-catch-block]
            {
            }
        }

        Task IBlockListener.BlockAddedAsync(Block block, int height, CancellationToken cancellationToken)
        {
            return this.watcher.ExecuteAsync(block, height, BlockEventType.Added, cancellationToken);
        }

        Task IBlockListener.BlockRemovingAsync(Block block, int height, CancellationToken cancellationToken)
        {
            return this.watcher.ExecuteAsync(block, height, BlockEventType.Removing, cancellationToken);
        }

        async Task<IEnumerable<ConfirmContext>> ITransactionConfirmationWatcherHandler<ConfirmContext>.CreateContextsAsync(Transaction tx, CancellationToken cancellationToken)
        {
            this.timerLock.EnterReadLock();

            try
            {
                if (this.timers.TryGetValue(tx.GetHash(), out var txTimers) && txTimers.Count > 0)
                {
                    var contexts = new Collection<ConfirmContext>();

                    foreach (var timer in txTimers)
                    {
                        var watch = await this.watchRepository.GetAsync(timer.Key, cancellationToken);
                        contexts.Add(watch);
                    }

                    return contexts;
                }
            }
            finally
            {
                this.timerLock.ExitReadLock();
            }

            return Enumerable.Empty<ConfirmContext>();
        }

        async Task<bool> IConfirmationWatcherHandler<TransactionWatch<ConfirmContext>, ConfirmContext>.ConfirmationUpdateAsync(TransactionWatch<ConfirmContext> watch, int confirmation, ConfirmationType type, CancellationToken cancellationToken)
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

                this.timerLock.EnterUpgradeableReadLock();
                try
                {
                    var requiredConfirmations = this.watches[watch.Context.Id].Context.Confirmation;

                    if (confirmation == requiredConfirmations)
                    {
                        await ConfirmAsync(watch, cancellationToken);
                        return true;
                    }
                }
                finally
                {
                    this.timerLock.ExitUpgradeableReadLock();
                }

                break;

            default:
                throw new NotSupportedException($"{nameof(ConfirmationType)} is not supported");
            }

            return false;
        }

        Task<IEnumerable<TransactionWatch<ConfirmContext>>> IConfirmationWatcherHandler<TransactionWatch<ConfirmContext>, ConfirmContext>.GetCurrentWatchesAsync(CancellationToken cancellationToken)
        {
            var watches = new Collection<TransactionWatch<ConfirmContext>>();
            foreach (var watch in this.watches)
            {
                watches.Add(watch.Value);
            }

            return Task.FromResult(watches.AsEnumerable());
        }

        async Task IWatcherHandler<TransactionWatch<ConfirmContext>, ConfirmContext>.AddWatchesAsync(IEnumerable<TransactionWatch<ConfirmContext>> watches, CancellationToken cancellationToken)
        {
            if (watches == null)
            {
                throw new ArgumentNullException(nameof(watches));
            }

            foreach (var watch in watches)
            {
                if (await StopTimer(watch.TransactionId, watch.Context.Id))
                {
                    this.watches.AddOrReplace(watch.Context.Id, watch);
                }
            }
        }

        async Task IWatcherHandler<TransactionWatch<ConfirmContext>, ConfirmContext>.RemoveWatchAsync(TransactionWatch<ConfirmContext> watch, WatchRemoveReason reason, CancellationToken cancellationToken)
        {
            this.watches.Remove(watch.Context.Id, out var transactionWatch);

            if (reason == WatchRemoveReason.BlockRemoved)
            {
                await ResumeTimerAsync(watch.Context.Id);
            }
            else if (reason == WatchRemoveReason.Completed)
            {
                RemoveTimer(watch.TransactionId, watch.Context.Id);
            }
        }
    }
}
