using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Ztm.Data.Entity.Contexts;
using Ztm.Data.Entity.Contexts.Main;
using Ztm.Zcoin.Watching;
using Rule = Ztm.WebApi.TransactionConfirmationWatchingRule<Ztm.WebApi.TransactionConfirmationCallbackResult>;

namespace Ztm.WebApi
{
    public sealed class SqlTransactionConfirmationWatchRepository : ITransactionConfirmationWatchRepository
    {
        readonly IMainDatabaseFactory db;

        public SqlTransactionConfirmationWatchRepository(IMainDatabaseFactory db)
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
                await db.AddAsync(new TransactionConfirmationWatch
                {
                    Id = watch.Id,
                    RuleId = watch.Context.Id,
                    StartBlock = watch.StartBlock,
                    StartTime = watch.StartTime,
                    Transaction = watch.TransactionId,
                    Status = (int)TransactionConfirmationWatchingWatchStatus.Pending,
                });

                await db.SaveChangesAsync(cancellationToken);
            }
        }

        public Task<IEnumerable<TransactionWatch<Rule>>> ListAsync(TransactionConfirmationWatchingWatchStatus status, CancellationToken cancellationToken)
        {
            using (var db = this.db.CreateDbContext())
            {
                return Task.FromResult<IEnumerable<TransactionWatch<Rule>>>
                (
                    db.TransactionConfirmationWatches
                        .Include(w => w.Rule)
                        .Where(w => (int)status == w.Status)
                        .Select(w => ToDomain(w))
                        .ToList()
                );
            }
        }

        public async Task UpdateStatusAsync(Guid id, TransactionConfirmationWatchingWatchStatus status, CancellationToken cancellationToken)
        {
            using (var db = this.db.CreateDbContext())
            {
                var watch = await db.TransactionConfirmationWatches.Where(w => w.Id == id).FirstOrDefaultAsync(cancellationToken);
                if (watch == null)
                {
                    throw new KeyNotFoundException("Watch id is not found.");
                }

                watch.Status = (int)status;

                await db.SaveChangesAsync(cancellationToken);
            }
        }

        TransactionWatch<Rule> ToDomain(TransactionConfirmationWatch watch)
        {
            return new TransactionWatch<Rule>
            (
                watch.Rule == null ? null : SqlTransactionConfirmationWatchingRuleRepository<TransactionConfirmationCallbackResult>.ToDomain(watch.Rule),
                watch.StartBlock,
                watch.Transaction,
                watch.StartTime,
                watch.Id
            );
        }
    }
}