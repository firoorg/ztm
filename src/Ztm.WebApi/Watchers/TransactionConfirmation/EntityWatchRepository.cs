using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NBitcoin;
using Ztm.Data.Entity.Contexts;
using DomainModel = Ztm.Zcoin.Watching.TransactionWatch<Ztm.WebApi.Watchers.TransactionConfirmation.Rule>;
using EntityModel = Ztm.Data.Entity.Contexts.Main.TransactionConfirmationWatcherWatch;

namespace Ztm.WebApi.Watchers.TransactionConfirmation
{
    public sealed class EntityWatchRepository : IWatchRepository
    {
        readonly IMainDatabaseFactory db;

        public EntityWatchRepository(IMainDatabaseFactory db)
        {
            if (db == null)
            {
                throw new ArgumentNullException(nameof(db));
            }

            this.db = db;
        }

        public async Task AddAsync(DomainModel watch, CancellationToken cancellationToken)
        {
            if (watch == null)
            {
                throw new ArgumentNullException(nameof(watch));
            }

            if (watch.Context == null)
            {
                throw new ArgumentException("Watch does not contain context.", nameof(watch));
            }

            using (var db = this.db.CreateDbContext())
            {
                await db.TransactionConfirmationWatcherWatches.AddAsync
                (
                    new EntityModel
                    {
                        Id = watch.Id,
                        RuleId = watch.Context.Id,
                        StartBlockHash = watch.StartBlock,
                        StartTime = watch.StartTime,
                        TransactionHash = watch.TransactionId,
                        Status = (int)WatchStatus.Pending,
                    }
                );

                await db.SaveChangesAsync(cancellationToken);
            }
        }

        public Task<IEnumerable<DomainModel>> ListPendingAsync(uint256 startBlock, CancellationToken cancellationToken)
        {
            return ListAsync(WatchStatus.Pending, startBlock, cancellationToken);
        }

        public Task<IEnumerable<DomainModel>> ListRejectedAsync(uint256 startBlock, CancellationToken cancellationToken)
        {
            return ListAsync(WatchStatus.Rejected, startBlock, cancellationToken);
        }

        public Task<IEnumerable<DomainModel>> ListSucceededAsync(
            uint256 startBlock,
            CancellationToken cancellationToken)
        {
            return ListAsync(WatchStatus.Success, startBlock, cancellationToken);
        }

        public async Task UpdateStatusAsync(Guid id, WatchStatus status, CancellationToken cancellationToken)
        {
            using (var db = this.db.CreateDbContext())
            using (var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken))
            {
                var watch = await db.TransactionConfirmationWatcherWatches
                    .Where(w => w.Id == id).FirstOrDefaultAsync(cancellationToken);

                if (watch == null)
                {
                    throw new KeyNotFoundException("Watch id is not found.");
                }

                if (watch.Status != (int)WatchStatus.Pending)
                {
                    throw new InvalidOperationException("The watch is not be able to update.");
                }

                switch (status)
                {
                    case WatchStatus.Rejected:
                    case WatchStatus.Success:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(status), "New status is not allowed to set.");
                }

                watch.Status = (int)status;
                await db.SaveChangesAsync(cancellationToken);

                tx.Commit();
            }
        }

        async Task<IEnumerable<DomainModel>> ListAsync(
            WatchStatus status,
            uint256 startBlock,
            CancellationToken cancellationToken)
        {
            IEnumerable<EntityModel> entities;

            using (var db = this.db.CreateDbContext())
            {
                IQueryable<EntityModel> query = db.TransactionConfirmationWatcherWatches
                    .Include(w => w.Rule)
                    .ThenInclude(r => r.Callback);

                query = (startBlock != null)
                    ? query.Where(w => w.Status == (int)status && w.StartBlockHash == startBlock)
                    : query.Where(w => w.Status == (int)status);

                entities = await query.ToListAsync(cancellationToken);
            }

            return entities.Select(e => ToDomain(e)).ToList();
        }

        DomainModel ToDomain(EntityModel entity)
        {
            return new DomainModel
            (
                EntityRuleRepository.ToDomain(entity.Rule),
                entity.StartBlockHash,
                entity.TransactionHash,
                entity.StartTime,
                entity.Id
            );
        }
    }
}
