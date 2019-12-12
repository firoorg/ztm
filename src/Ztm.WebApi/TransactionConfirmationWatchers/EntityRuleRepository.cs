using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using NBitcoin;
using Newtonsoft.Json;
using Ztm.Data.Entity.Contexts;
using TransactionConfirmationWatchingRuleModel = Ztm.Data.Entity.Contexts.Main.TransactionConfirmationWatchingRule;
using Ztm.WebApi.Callbacks;

namespace Ztm.WebApi.TransactionConfirmationWatchers
{
    public class EntityRuleRepository : IRuleRepository
    {
        readonly IMainDatabaseFactory db;

        public EntityRuleRepository(IMainDatabaseFactory db)
        {
            if (db == null)
            {
                throw new ArgumentNullException(nameof(db));
            }

            this.db = db;
        }

        public async Task<Rule> AddAsync(
            uint256 transaction, int confirmations, TimeSpan waitingTime, CallbackResult successResponse, CallbackResult timeoutResponse, Callback callback, CancellationToken cancellationToken)
        {
            if (transaction == null)
            {
                throw new ArgumentNullException(nameof(transaction));
            }

            if (successResponse == null)
            {
                throw new ArgumentNullException(nameof(successResponse));
            }

            if (timeoutResponse == null)
            {
                throw new ArgumentNullException(nameof(timeoutResponse));
            }

            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            using (var db = this.db.CreateDbContext())
            {
                var watch = await db.TransactionConfirmationWatchingRules.AddAsync
                (
                    new TransactionConfirmationWatchingRuleModel
                    {
                        Id = Guid.NewGuid(),
                        CallbackId = callback.Id,
                        Transaction = transaction,
                        Status = (int)RuleStatus.Pending,
                        Confirmation = confirmations,
                        OriginalWaitingTime = waitingTime,
                        RemainingWaitingTime = waitingTime,
                        SuccessData = JsonConvert.SerializeObject(successResponse),
                        TimeoutData = JsonConvert.SerializeObject(timeoutResponse),
                        CurrentWatchId = null,
                    },
                    cancellationToken
                );

                await db.SaveChangesAsync(cancellationToken);

                return ToDomain(watch.Entity, callback);
            }
        }

        public async Task ClearCurrentWatchIdAsync(Guid id, CancellationToken cancellationToken)
        {
            using (var db = this.db.CreateDbContext())
            {
                var rule = await db.TransactionConfirmationWatchingRules
                    .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

                if (rule == null)
                {
                    throw new KeyNotFoundException("The rule id is not existed.");
                }

                rule.CurrentWatchId = null;
                await db.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task<Rule> GetAsync(Guid id, CancellationToken cancellationToken)
        {
            using (var db = this.db.CreateDbContext())
            {
                var rule = await db.TransactionConfirmationWatchingRules
                    .Include(e => e.Callback)
                    .Include(e => e.CurrentWatch)
                    .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

                return rule == null ? null : ToDomain(rule);
            }
        }

        public async Task<TimeSpan> GetRemainingWaitingTimeAsync(Guid id, CancellationToken cancellationToken)
        {
            using (var db = this.db.CreateDbContext())
            {
                var rule = await db.TransactionConfirmationWatchingRules
                    .Include(e => e.Callback)
                    .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

                if (rule == null)
                {
                    throw new KeyNotFoundException("The rule id is not found.");
                }

                return rule.RemainingWaitingTime;
            }
        }

        public async Task<RuleStatus> GetStatusAsync(Guid id, CancellationToken cancellationToken)
        {
            using (var db = this.db.CreateDbContext())
            {
                var rule = await db.TransactionConfirmationWatchingRules
                    .Include(e => e.Callback)
                    .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

                if (rule == null)
                {
                    throw new KeyNotFoundException("The rule id is not found.");
                }

                return (RuleStatus)rule.Status;
            }
        }

        public async Task<IEnumerable<Rule>> ListWaitingAsync(CancellationToken cancellationToken)
        {
            using (var db = this.db.CreateDbContext())
            {
                var rules = await db.TransactionConfirmationWatchingRules
                    .Include(e => e.Callback)
                    .Include(e => e.CurrentWatch)
                    .Where(e => e.Status == (int)WatchStatus.Pending && e.CurrentWatchId == null)
                    .ToListAsync(cancellationToken);

                return rules.Select(e => ToDomain(e));
            }
        }

        public async Task SubtractRemainingWaitingTimeAsync(Guid id, TimeSpan consumedTime, CancellationToken cancellationToken)
        {
            using (var db = this.db.CreateDbContext())
            {
                var rule = await db.TransactionConfirmationWatchingRules
                    .Include(e => e.Callback)
                    .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

                if (rule == null)
                {
                    throw new KeyNotFoundException("The rule id is not found.");
                }

                if (consumedTime < TimeSpan.Zero)
                {
                    throw new ArgumentException("The consumed time is negative.");
                }

                rule.RemainingWaitingTime -= consumedTime;
                rule.RemainingWaitingTime = rule.RemainingWaitingTime < TimeSpan.Zero
                    ? TimeSpan.Zero
                    : rule.RemainingWaitingTime;

                await db.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task UpdateCurrentWatchIdAsync(Guid id, Guid watchId, CancellationToken cancellationToken)
        {
            using (var db = this.db.CreateDbContext())
            using (var tx = await db.Database.BeginTransactionAsync(cancellationToken))
            {
                var watch = await db.TransactionConfirmationWatches
                    .FirstOrDefaultAsync(w => w.Id == watchId, cancellationToken);

                if (watch == null)
                {
                    throw new KeyNotFoundException("The watch id is not found.");
                }

                var rule = await db.TransactionConfirmationWatchingRules
                    .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

                if (rule == null)
                {
                    throw new KeyNotFoundException("The rule id is not found.");
                }

                rule.CurrentWatchId = watchId;

                await db.SaveChangesAsync(cancellationToken);
                tx.Commit();
            }
        }

        public async Task UpdateStatusAsync(Guid id, RuleStatus status, CancellationToken cancellationToken)
        {
            using (var db = this.db.CreateDbContext())
            {
                var rule = await db.TransactionConfirmationWatchingRules
                    .Include(e => e.Callback)
                    .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

                if (rule == null)
                {
                    throw new KeyNotFoundException("The rule id is not found.");
                }

                rule.Status = (int)status;

                await db.SaveChangesAsync(cancellationToken);
            }
        }

        public static Rule ToDomain(
            Ztm.Data.Entity.Contexts.Main.TransactionConfirmationWatchingRule rule,
            Callback callback = null)
        {
            return new Rule(
                rule.Id,
                rule.Transaction,
                rule.Confirmation,
                rule.OriginalWaitingTime,
                JsonConvert.DeserializeObject(rule.SuccessData),
                JsonConvert.DeserializeObject(rule.TimeoutData),
                callback != null
                    ? callback
                    : (rule.Callback == null ? null : EntityCallbackRepository.ToDomain(rule.Callback))
            );
        }
    }
}