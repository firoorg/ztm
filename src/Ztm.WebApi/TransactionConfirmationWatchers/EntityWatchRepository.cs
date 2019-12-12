using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Ztm.Data.Entity.Contexts;
using Ztm.Data.Entity.Contexts.Main;
using Ztm.Zcoin.Watching;

namespace Ztm.WebApi.TransactionConfirmationWatchers
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

        public async Task AddAsync(TransactionWatch<Rule> watch, CancellationToken cancellationToken)
        {
            if (watch == null)
            {
                throw new ArgumentNullException(nameof(watch));
            }

            if (watch.Context == null)
            {
                throw new ArgumentNullException(nameof(watch.Context));
            }

            using (var db = this.db.CreateDbContext())
            {
                await db.TransactionConfirmationWatches.AddAsync
                (
                    new TransactionConfirmationWatch
                    {
                        Id = watch.Id,
                        RuleId = watch.Context.Id,
                        StartBlock = watch.StartBlock,
                        StartTime = watch.StartTime,
                        Transaction = watch.TransactionId,
                        Status = (int)WatchStatus.Pending,
                    }
                );

                await db.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task<IEnumerable<TransactionWatch<Rule>>> ListAsync(WatchStatus status, CancellationToken cancellationToken)
        {
            using (var db = this.db.CreateDbContext())
            {
                return await db.TransactionConfirmationWatches
                    .Where(w => (int)status == w.Status)
                    .Select(w => ToDomain(w))
                    .ToListAsync(cancellationToken);
            }
        }

        public async Task UpdateStatusAsync(Guid id, WatchStatus status, CancellationToken cancellationToken)
        {
            using (var db = this.db.CreateDbContext())
            {
                var watch = await db.TransactionConfirmationWatches
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
                        throw new InvalidOperationException("New status is not allowed to set.");

                }

                watch.Status = (int)status;
                await db.SaveChangesAsync(cancellationToken);
            }
        }

        TransactionWatch<Rule> ToDomain(TransactionConfirmationWatch watch)
        {
            return new TransactionWatch<Rule>
            (
                watch.Rule == null ? null : EntityRuleRepository.ToDomain(watch.Rule),
                watch.StartBlock,
                watch.Transaction,
                watch.StartTime,
                watch.Id
            );
        }
    }
}