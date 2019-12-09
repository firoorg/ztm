using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Ztm.Data.Entity.Contexts;
using Ztm.Data.Entity.Contexts.Main;
using Ztm.Zcoin.Watching;
using Rule = Ztm.WebApi.TransactionConfirmationWatchers.TransactionConfirmationWatchingRule<Ztm.WebApi.TransactionConfirmationWatchers.TransactionConfirmationCallbackResult>;

namespace Ztm.WebApi.TransactionConfirmationWatchers
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
            using (var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken))
            {
                await db.TransactionConfirmationWatches.AddAsync(
                    new TransactionConfirmationWatch
                    {
                        Id = watch.Id,
                        RuleId = watch.Context.Id,
                        StartBlock = watch.StartBlock,
                        StartTime = watch.StartTime,
                        Transaction = watch.TransactionId,
                        Status = (int)TransactionConfirmationWatchingWatchStatus.Pending,
                    }
                );

                var rule = await db.TransactionConfirmationWatchingRules
                    .FirstAsync(r => r.Id == watch.Context.Id);
                if (rule.CurrentWatchId != null)
                {
                    throw new InvalidOperationException("The rule is watched.");
                }

                rule.CurrentWatchId = watch.Id;

                await db.SaveChangesAsync(cancellationToken);
                tx.Commit();
            }
        }

        public Task<IEnumerable<TransactionWatch<Rule>>> ListAsync(TransactionConfirmationWatchingWatchStatus status, CancellationToken cancellationToken)
        {
            using (var db = this.db.CreateDbContext())
            {
                return Task.FromResult<IEnumerable<TransactionWatch<Rule>>>
                (
                    db.TransactionConfirmationWatches
                        .Where(w => (int)status == w.Status)
                        .Select(w => ToDomain(w))
                        .ToList()
                );
            }
        }

        public async Task UpdateStatusAsync(Guid id, TransactionConfirmationWatchingWatchStatus status, CancellationToken cancellationToken)
        {
            using (var db = this.db.CreateDbContext())
            using (var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken))
            {
                var watch = await db.TransactionConfirmationWatches.Where(w => w.Id == id).FirstOrDefaultAsync(cancellationToken);
                if (watch == null)
                {
                    throw new KeyNotFoundException("Watch id is not found.");
                }

                if (watch.Status != (int)TransactionConfirmationWatchingWatchStatus.Pending)
                {
                    throw new InvalidOperationException("The watch is not be able to update.");
                }

                switch (status)
                {
                    case TransactionConfirmationWatchingWatchStatus.Error:
                    case TransactionConfirmationWatchingWatchStatus.Rejected:
                    case TransactionConfirmationWatchingWatchStatus.Success:
                        break;
                    default:
                        throw new InvalidOperationException("New status is not allowed to set.");

                }

                watch.Status = (int)status;

                var rule = await db.TransactionConfirmationWatchingRules.FirstAsync(r => r.Id == watch.RuleId);
                rule.CurrentWatchId = null;

                await db.SaveChangesAsync(cancellationToken);
                tx.Commit();
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