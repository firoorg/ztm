using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using Ztm.Threading;
using Ztm.Zcoin.Synchronization.Watchers;

namespace Ztm.WebApi
{
    using ConfirmContext = TransactionConfirmationWatch<TransactionConfirmationCallbackResult>;

    public class TransactionConfirmationWatcherHandler : ITransactionConfirmationWatcherHandler<ConfirmContext>
    {
        readonly ICallbackRepository callbackRepository;
        readonly ITransactionConfirmationWatchRepository<TransactionConfirmationCallbackResult> watchRepository;
        readonly ICallbackExecuter callbackExecuter;

        readonly ReaderWriterLockSlim timerLock;
        Dictionary<uint256, Dictionary<Guid, Tuple<Ztm.Threading.Timer, ConfirmContext>>> timers;

        Dictionary<Guid, TransactionWatch<ConfirmContext>> watches;

        public TransactionConfirmationWatcherHandler(
            ICallbackRepository callbackRepository,
            ITransactionConfirmationWatchRepository<TransactionConfirmationCallbackResult> watchRepository,
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

            if (callbackExecuter == null)
            {
                throw new ArgumentNullException(nameof(callbackExecuter));
            }

            this.callbackRepository = callbackRepository;
            this.watchRepository = watchRepository;
            this.callbackExecuter = callbackExecuter;

            timerLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
            timers = new Dictionary<uint256, Dictionary<Guid, Tuple<Threading.Timer, ConfirmContext>>>();
            watches = new Dictionary<Guid, TransactionWatch<ConfirmContext>>();
        }

        public async Task Initialize(CancellationToken cancellationToken)
        {
            var watches = await this.watchRepository.ListAsync(cancellationToken);
            foreach (var watch in watches)
            {
                if (!watch.Callback.Completed)
                {
                    TimerSetup(watch);
                }
            }
        }

        public async Task StopAllTimers(CancellationToken cancellationToken)
        {
            timerLock.EnterWriteLock();

            try
            {
                foreach (var timerSet in timers)
                {
                    foreach (var timer in timerSet.Value)
                    {
                        await timer.Value.Item1.StopAsync(cancellationToken);
                    }
                }
            }
            finally
            {
                timerLock.ExitWriteLock();
            }
        }

        public async Task<ConfirmContext> AddTransactionAsync(
            uint256 transaction,
            int confirmation,
            TimeSpan timeout,
            IPAddress registeringIp,
            Uri callbackUrl,
            TransactionConfirmationCallbackResult successData,
            TransactionConfirmationCallbackResult timeoutData,
            CancellationToken cancellationToken)
        {
            var callback = await this.callbackRepository.AddAsync
            (
                registeringIp,
                callbackUrl,
                cancellationToken
            );

            var watch = await this.watchRepository.AddAsync
            (
                transaction,
                confirmation,
                timeout,
                successData,
                timeoutData,
                callback,
                cancellationToken
            );

            TimerSetup(watch);

            return watch;
        }

        void TimerSetup(TransactionConfirmationWatch<TransactionConfirmationCallbackResult> watch)
        {
            var timer = new Ztm.Threading.Timer();

            timerLock.EnterWriteLock();

            try
            {
                if (!timers.ContainsKey(watch.Transaction))
                {
                    timers[watch.Transaction] = new Dictionary<Guid, Tuple<Threading.Timer, ConfirmContext>>();
                }

                timers[watch.Transaction][watch.Id] = new Tuple<Threading.Timer, ConfirmContext>(timer, watch);
                timer.Elapsed += OnTimeout;
                var due = watch.Due - DateTime.UtcNow;
                timer.Start(due < TimeSpan.Zero ? TimeSpan.Zero : due, null, watch.Id);
            }
            finally
            {
                timerLock.ExitWriteLock();
            }
        }

        protected virtual async void OnTimeout(object sender, TimerElapsedEventArgs e)
        {
            var watch = await watchRepository.GetAsync((Guid)e.Context, CancellationToken.None);

            await Execute(watch.Callback, watch.Timeout, CancellationToken.None);
            RemoveTimer(watch.Transaction, watch.Id);
        }

        void RemoveTimer(uint256 transaction, Guid id)
        {
            timerLock.EnterWriteLock();

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
                timerLock.ExitWriteLock();
            }
        }

        async Task<bool> StopTimer(uint256 transaction, Guid id)
        {
            timerLock.EnterWriteLock();
            try
            {
                if (timers.ContainsKey(transaction))
                {
                    var txTimers = timers[transaction];
                    if (txTimers.ContainsKey(id))
                    {
                        var timer = txTimers[id].Item1;
                        await timer.StopAsync(CancellationToken.None);
                        if (timer.ElapsedCount == 0) // Not Timeout
                        {
                            RemoveTimer(transaction, id);
                            return true;
                        }
                    }
                }
            }
            finally
            {
                timerLock.ExitWriteLock();
            }

            return false;
        }

        void ResumeTimer(ConfirmContext watch)
        {
            timerLock.EnterWriteLock();

            try
            {
                TimerSetup(watch);
            }
            finally
            {
                timerLock.EnterWriteLock();
            }
        }

        async Task Confirm(TransactionWatch<ConfirmContext> watch)
        {
            await Execute(watch.Context.Callback, watch.Context.Success, CancellationToken.None);
        }

        async Task Execute(Callback callback, TransactionConfirmationCallbackResult payload, CancellationToken cancellationToken)
        {
            await this.callbackExecuter.Execute(callback.Url, payload);
            await callbackRepository.AddHistoryAsync(callback.Id, payload, cancellationToken);
            await callbackRepository.SetCompletedAsyc(callback.Id, cancellationToken);
        }

        public async Task<bool> ConfirmationUpdateAsync(TransactionWatch<ConfirmContext> watch, int confirmation, ConfirmationType type, CancellationToken cancellationToken)
        {
            if (type == ConfirmationType.Unconfirming)
            {
                if (confirmation == 1)
                {
                    ResumeTimer(watch.Context);
                    return false;
                }
            }
            else
            {
                if (confirmation == 1)
                {
                    if (!(await StopTimer(watch.TransactionId, watch.Context.Id)))
                    {
                        return false;
                    }
                }

                if (confirmation == watch.Context.Confirmation)
                {
                    await Confirm(watch);
                    return true;
                }
            }

            return false;
        }

        public Task<IEnumerable<ConfirmContext>> CreateContextsAsync(Transaction tx, CancellationToken cancellationToken)
        {
            timerLock.EnterReadLock();

            try
            {
                if (timers.ContainsKey(tx.GetHash()))
                {
                    return Task.FromResult(timers[tx.GetHash()].Select(t => t.Value.Item2));
                }
            }
            finally
            {
                timerLock.ExitReadLock();
            }

            return Task.FromResult((IEnumerable<ConfirmContext>)(new Collection<ConfirmContext>()));
        }

        public Task<IEnumerable<TransactionWatch<ConfirmContext>>> GetCurrentWatchesAsync(CancellationToken cancellationToken)
        {
            var watches = new Collection<TransactionWatch<ConfirmContext>>();
            foreach (var watch in this.watches)
            {
                watches.Add(watch.Value);
            }

            return Task.FromResult(watches.AsEnumerable());
        }

        public Task AddWatchesAsync(IEnumerable<TransactionWatch<ConfirmContext>> watches, CancellationToken cancellationToken)
        {
            if (watches == null)
            {
                throw new ArgumentNullException(nameof(watches));
            }

            foreach (var watch in watches)
            {
                this.watches.Add(watch.Context.Id, watch);
            }

            return Task.Delay(0);
        }

        public Task RemoveWatchAsync(TransactionWatch<ConfirmContext> watch, WatchRemoveReason reason, CancellationToken cancellationToken)
        {
            this.watches.Remove(watch.Context.Id);

            return Task.Delay(0);
        }
    }
}