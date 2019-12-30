using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using NBitcoin;
using Newtonsoft.Json;
using Ztm.Data.Entity.Contexts;
using Ztm.WebApi.Callbacks;
using EntityModel = Ztm.Data.Entity.Contexts.Main.TransactionConfirmationWatcherRule;
using System.Text;
using System.IO;

namespace Ztm.WebApi.Watchers.TransactionConfirmation
{
    public class EntityRuleRepository : IRuleRepository
    {
        readonly IMainDatabaseFactory db;
        readonly JsonSerializer serializer;

        public EntityRuleRepository(IMainDatabaseFactory db, JsonSerializer serializer)
        {
            if (db == null)
            {
                throw new ArgumentNullException(nameof(db));
            }

            if (serializer == null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }

            this.db = db;
            this.serializer = serializer;
        }

        public static Rule ToDomain(JsonSerializer serializer, EntityModel rule, Callback callback = null)
        {
            using (var successReader = new JsonTextReader(new StringReader(rule.SuccessData)))
            using (var timeoutReader = new JsonTextReader(new StringReader(rule.TimeoutData)))
            {
                return new Rule
                (
                    rule.Id,
                    rule.TransactionHash,
                    rule.Confirmation,
                    rule.OriginalWaitingTime,
                    serializer.Deserialize<CallbackResult>(successReader),
                    serializer.Deserialize<CallbackResult>(timeoutReader),
                    callback != null
                        ? callback
                        : (rule.Callback == null ? null : EntityCallbackRepository.ToDomain(rule.Callback)),
                    DateTime.SpecifyKind(rule.CreatedAt, DateTimeKind.Utc)
                );
            }
        }

        public async Task<Rule> AddAsync(
            uint256 transaction, int confirmations, TimeSpan waitingTime, CallbackResult successResponse,
            CallbackResult timeoutResponse, Callback callback, CancellationToken cancellationToken)
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

            var successStringBuilder = new StringBuilder();
            var timeoutStringBuilder = new StringBuilder();

            using (var successWriter = new StringWriter(successStringBuilder))
            using (var timeoutWriter = new StringWriter(timeoutStringBuilder))
            {
                this.serializer.Serialize(successWriter, successResponse);
                this.serializer.Serialize(timeoutWriter, timeoutResponse);
            }

            using (var db = this.db.CreateDbContext())
            {
                var watch = await db.TransactionConfirmationWatcherRules.AddAsync
                (
                    new EntityModel
                    {
                        Id = Guid.NewGuid(),
                        CallbackId = callback.Id,
                        TransactionHash = transaction,
                        Status = (int)RuleStatus.Pending,
                        Confirmation = confirmations,
                        OriginalWaitingTime = waitingTime,
                        RemainingWaitingTime = waitingTime,
                        SuccessData = successStringBuilder.ToString(),
                        TimeoutData = timeoutStringBuilder.ToString(),
                        CurrentWatchId = null,
                        CreatedAt = DateTime.UtcNow,
                    },
                    cancellationToken
                );

                await db.SaveChangesAsync(cancellationToken);

                return ToDomain(this.serializer, watch.Entity, callback);
            }
        }

        public async Task<Rule> GetAsync(Guid id, CancellationToken cancellationToken)
        {
            using (var db = this.db.CreateDbContext())
            {
                var rule = await db.TransactionConfirmationWatcherRules
                    .Include(e => e.Callback)
                    .Include(e => e.CurrentWatch)
                    .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

                return rule == null ? null : ToDomain(this.serializer, rule);
            }
        }

        public async Task<TimeSpan> GetRemainingWaitingTimeAsync(Guid id, CancellationToken cancellationToken)
        {
            using (var db = this.db.CreateDbContext())
            {
                var rule = await db.TransactionConfirmationWatcherRules
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
                var rule = await db.TransactionConfirmationWatcherRules
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
                var rules = await db.TransactionConfirmationWatcherRules
                    .Include(e => e.Callback)
                    .Include(e => e.CurrentWatch)
                    .Where(e => e.Status == (int)RuleStatus.Pending && e.CurrentWatchId == null)
                    .ToListAsync(cancellationToken);

                return rules.Select(e => ToDomain(this.serializer, e)).ToList();
            }
        }

        public async Task SubtractRemainingWaitingTimeAsync(Guid id, TimeSpan consumedTime, CancellationToken cancellationToken)
        {
            if (consumedTime <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(consumedTime), "The consumed time is negative or zero.");
            }

            using (var db = this.db.CreateDbContext())
            using (var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken))
            {
                var rule = await db.TransactionConfirmationWatcherRules
                    .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

                if (rule == null)
                {
                    throw new KeyNotFoundException("The rule id is not found.");
                }

                rule.RemainingWaitingTime -= consumedTime;
                rule.RemainingWaitingTime = rule.RemainingWaitingTime < TimeSpan.Zero
                    ? TimeSpan.Zero
                    : rule.RemainingWaitingTime;

                await db.SaveChangesAsync(cancellationToken);
                tx.Commit();
            }
        }

        public async Task UpdateCurrentWatchAsync(Guid id, Guid? watchId, CancellationToken cancellationToken)
        {
            using (var db = this.db.CreateDbContext())
            using (var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken))
            {
                var rule = await db.TransactionConfirmationWatcherRules
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
                var rule = await db.TransactionConfirmationWatcherRules
                    .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

                if (rule == null)
                {
                    throw new KeyNotFoundException("The rule id is not found.");
                }

                rule.Status = (int)status;

                await db.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
