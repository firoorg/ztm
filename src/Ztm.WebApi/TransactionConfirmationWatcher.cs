using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using NBitcoin;
using Ztm.Threading;
using Ztm.Zcoin.Synchronization;
using Ztm.Zcoin.Watching;

namespace Ztm.WebApi
{
    using ConfirmContext = TransactionConfirmationWatch<TransactionConfirmationCallbackResult>;
    using Timer = Ztm.Threading.Timer;

    public sealed class TransactionConfirmationWatcher : ITransactionConfirmationWatcher, IHostedService, IBlockListener, ITransactionConfirmationWatcherHandler<Guid>
    {
        readonly Ztm.Zcoin.Watching.TransactionConfirmationWatcher<Guid> watcher;

        // Providers
        readonly ICallbackRepository callbackRepository;
        readonly ITransactionConfirmationWatchRepository<TransactionConfirmationCallbackResult> watchRepository;
        readonly ICallbackExecuter callbackExecuter;

        // State recorders
        readonly ReaderWriterLockSlim timerLock;

        // Dictionary from transaction to Dictionary from watch id to timer and confirmations.
        readonly Dictionary<uint256, Dictionary<Guid, Tuple<Timer, int>>> timers;

        readonly ConcurrentDictionary<Guid, TransactionWatch<Guid>> watches;

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

            this.watcher = new Zcoin.Watching.TransactionConfirmationWatcher<Guid>
            (
                this,
                blocks
            );

            this.timerLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
            timers = new Dictionary<uint256, Dictionary<Guid, Tuple<Timer, int>>>();
            watches = new ConcurrentDictionary<Guid, TransactionWatch<Guid>>();
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
                throw new ArgumentOutOfRangeException("Confirmation is less than zero");
            }

            if (!Timer.IsValidDuration(unconfirmedWaitingTime))
            {
                throw new ArgumentOutOfRangeException("UnconfirmedWaitingTime is invalid duration");
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

            foreach (var watch in allWatches.Where(w => !w.Callback.Completed))
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
                    foreach (var timer in timerSet.Value.Where(t => t.Value.Item1.Status == TimerStatus.Started))
                    {
                        await timer.Value.Item1.StopAsync(cancellationToken);
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

            var watch = await this.watchRepository.GetAsync(id, CancellationToken.None);
            try
            {
                if (!timers.ContainsKey(watch.Transaction))
                {
                    timers[watch.Transaction] = new Dictionary<Guid, Tuple<Timer, int>>();
                }

                timers[watch.Transaction][watch.Id] = new Tuple<Timer, int>(timer, watch.Confirmation);
                timer.Elapsed += OnTimeout;
                timer.Start(watch.RemainingWaitingTime < TimeSpan.Zero ? TimeSpan.Zero : watch.RemainingWaitingTime, null, watch.Id);
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

                await ExecuteCallbackAsync(watch.Callback, watch.Timeout, cancellationToken);
                RemoveTimer(watch.Transaction, watch.Id);
            });
        }

        void RemoveTimer(uint256 transaction, Guid id)
        {
            this.timerLock.EnterWriteLock();

            try
            {
                timers[transaction].Remove(id);
                if (timers[transaction].Count == 0)
                {
                    timers.Remove(transaction);
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
                    await timer.Item1.StopAsync(CancellationToken.None);
                    if (timer.Item1.ElapsedCount == 0)
                    {
                        var watchData = await this.watchRepository.GetAsync(id, CancellationToken.None);

                        await UpdateRemainingWaitingTimeAsync(
                            id,
                            watchData.RemainingWaitingTime - timer.Item1.ElapsedTime,
                            CancellationToken.None);

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
                await SetupTimerAsync(id);
            }
            finally
            {
                this.timerLock.EnterWriteLock();
            }
        }

        async Task ConfirmAsync(TransactionWatch<Guid> watch)
        {
            var watchObject = await this.watchRepository.GetAsync(watch.Context, CancellationToken.None);

            await ExecuteCallbackAsync(watchObject.Callback, watchObject.Success, CancellationToken.None);
            RemoveTimer(watch.TransactionId, watch.Context);
        }

        async Task ExecuteCallbackAsync(Callback callback, TransactionConfirmationCallbackResult payload, CancellationToken cancellationToken)
        {
            await this.callbackRepository.AddHistoryAsync(callback.Id, payload, cancellationToken);

            if (await this.callbackExecuter.Execute(callback.Id, callback.Url, payload, cancellationToken))
            {
                await this.callbackRepository.SetCompletedAsyc(callback.Id, cancellationToken);
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

        Task<IEnumerable<Guid>> ITransactionConfirmationWatcherHandler<Guid>.CreateContextsAsync(Transaction tx, CancellationToken cancellationToken)
        {
            this.timerLock.EnterReadLock();

            try
            {
                if (this.timers.TryGetValue(tx.GetHash(), out var txTimers))
                {
                    return Task.FromResult((IEnumerable<Guid>)txTimers.Select(t => t.Key).ToList());
                }
            }
            finally
            {
                this.timerLock.ExitReadLock();
            }

            return Task.FromResult(Enumerable.Empty<Guid>());
        }

        async Task<bool> IConfirmationWatcherHandler<TransactionWatch<Guid>, Guid>.ConfirmationUpdateAsync(TransactionWatch<Guid> watch, int confirmation, ConfirmationType type, CancellationToken cancellationToken)
        {
            switch (type)
            {
            case ConfirmationType.Unconfirming:
                if (confirmation == 1)
                {
                    await ResumeTimerAsync(watch.Context);
                    return false;
                }
                break;

            case ConfirmationType.Confirmed:
                if (confirmation == 1 && !(await StopTimer(watch.TransactionId, watch.Context)))
                {
                    return false;
                }

                this.timerLock.EnterUpgradeableReadLock();
                try
                {
                    var requiredConfirmations = this.timers[watch.TransactionId][watch.Context].Item2;

                    if (confirmation == requiredConfirmations)
                    {
                        await ConfirmAsync(watch);
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

        Task<IEnumerable<TransactionWatch<Guid>>> IConfirmationWatcherHandler<TransactionWatch<Guid>, Guid>.GetCurrentWatchesAsync(CancellationToken cancellationToken)
        {
            var watches = new Collection<TransactionWatch<Guid>>();
            foreach (var watch in this.watches)
            {
                watches.Add(watch.Value);
            }

            return Task.FromResult(watches.AsEnumerable());
        }

        Task IWatcherHandler<TransactionWatch<Guid>, Guid>.AddWatchesAsync(IEnumerable<TransactionWatch<Guid>> watches, CancellationToken cancellationToken)
        {
            if (watches == null)
            {
                throw new ArgumentNullException(nameof(watches));
            }

            foreach (var watch in watches)
            {
                this.watches.AddOrReplace(watch.Context, watch);
            }

            return Task.FromResult(0);
        }

        Task IWatcherHandler<TransactionWatch<Guid>, Guid>.RemoveWatchAsync(TransactionWatch<Guid> watch, WatchRemoveReason reason, CancellationToken cancellationToken)
        {
            this.watches.Remove(watch.Context, out var transactionWatch);

            return Task.FromResult(0);
        }
    }
}
