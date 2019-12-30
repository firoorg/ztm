using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using Ztm.WebApi.Watchers.TransactionConfirmation;
using Watch = Ztm.Zcoin.Watching.TransactionWatch<Ztm.WebApi.Watchers.TransactionConfirmation.Rule>;
using WatchStatus = Ztm.Data.Entity.Contexts.Main.TransactionConfirmationWatcherWatchStatus;

namespace Ztm.WebApi.Tests.Watchers.TransactionConfirmation
{
    sealed class FakeWatchRepository : IWatchRepository
    {
        readonly Dictionary<Guid, TransactionWatchWithStatus> watches;

        public FakeWatchRepository()
        {
            this.watches = new Dictionary<Guid, TransactionWatchWithStatus>();
        }

        public Task AddAsync(Watch watch, CancellationToken cancellationToken)
        {
            this.watches.Add(watch.Id, new TransactionWatchWithStatus
            {
                Watch = watch,
                Status = WatchStatus.Pending
            });

            return Task.FromResult(watch);
        }

        public Task<IEnumerable<Watch>> ListPendingAsync(uint256 startBlock, CancellationToken cancellationToken)
        {
            var result = this.watches
                .Where(w => w.Value.Status == WatchStatus.Pending && (startBlock == null || w.Value.Watch.StartBlock == startBlock))
                .Select(w => w.Value.Watch);

            return Task.FromResult(result);
        }

        public Task<IEnumerable<Watch>> ListRejectedAsync(uint256 startBlock, CancellationToken cancellationToken)
        {
            var result = this.watches
                .Where(w => w.Value.Status == WatchStatus.Rejected && (startBlock == null || w.Value.Watch.StartBlock == startBlock))
                .Select(w => w.Value.Watch);

            return Task.FromResult(result);
        }

        public Task<IEnumerable<Watch>> ListSucceededAsync(uint256 startBlock, CancellationToken cancellationToken)
        {
            var result = this.watches
                .Where(w => w.Value.Status == WatchStatus.Succeeded && (startBlock == null || w.Value.Watch.StartBlock == startBlock))
                .Select(w => w.Value.Watch);

            return Task.FromResult(result);
        }

        public Task SetRejectedAsync(Guid id, CancellationToken cancellationToken)
        {
            return UpdateStatusAsync(id, WatchStatus.Rejected, cancellationToken);
        }

        public Task SetSucceededAsync(Guid id, CancellationToken cancellationToken)
        {
            return UpdateStatusAsync(id, WatchStatus.Succeeded, cancellationToken);
        }

        Task UpdateStatusAsync(Guid id, WatchStatus status, CancellationToken cancellationToken)
        {
            if (this.watches.TryGetValue(id, out var watchWithStatus))
            {
                watchWithStatus.Status = status;
                return Task.CompletedTask;
            }

            throw new KeyNotFoundException("Watch Id is not exist.");
        }

        sealed class TransactionWatchWithStatus
        {
            public WatchStatus Status { get; set; }
            public Watch Watch { get; set; }
        }
    }
}
