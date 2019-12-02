using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using NBitcoin;
using Newtonsoft.Json;
using Ztm.Data.Entity.Contexts;

namespace Ztm.WebApi
{
    public class SqlTransactionConfirmationWatchingRuleRepository<TCallbackResult> : ITransactionConfirmationWatchingRuleRepository<TCallbackResult>
    {
        readonly IMainDatabaseFactory db;

        public SqlTransactionConfirmationWatchingRuleRepository(IMainDatabaseFactory db)
        {
            if (db == null)
            {
                throw new ArgumentNullException(nameof(db));
            }

            this.db = db;
        }

        public async Task<TransactionConfirmationWatchingRule<TCallbackResult>> AddAsync(
            uint256 transaction, int confirmation, TimeSpan waitingTime, TCallbackResult successData, TCallbackResult timeoutData, Callback callback, CancellationToken cancellationToken)
        {
            if (transaction == null)
            {
                throw new ArgumentNullException(nameof(transaction));
            }

            if (successData == null)
            {
                throw new ArgumentNullException(nameof(successData));
            }

            if (timeoutData == null)
            {
                throw new ArgumentNullException(nameof(timeoutData));
            }

            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            using (var db = this.db.CreateDbContext())
            {
                var watch = await db.TransactionConfirmationWatchingRules
                    .AddAsync(
                    new Ztm.Data.Entity.Contexts.Main.TransactionConfirmationWatchingRule
                    {
                        Id = Guid.NewGuid(),
                        CallbackId = callback.Id,
                        Transaction = transaction,
                        Status = (int)TransactionConfirmationWatchingRuleStatus.Pending,
                        Confirmation = confirmation,
                        WaitingTime = waitingTime,
                        RemainingWaitingTime = waitingTime,
                        SuccessData = JsonConvert.SerializeObject(successData),
                        TimeoutData = JsonConvert.SerializeObject(timeoutData),
                    }, cancellationToken);

                await db.SaveChangesAsync(cancellationToken);

                return ToDomain(watch.Entity, callback);
            }
        }

        public async Task<TransactionConfirmationWatchingRule<TCallbackResult>> GetAsync(Guid id, CancellationToken cancellationToken)
        {
            using (var db = this.db.CreateDbContext())
            {
                var watch = await db.TransactionConfirmationWatchingRules
                    .Include(e => e.Callback)
                    .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

                return watch == null ? null : ToDomain(watch);
            }
        }

        public async Task<IEnumerable<TransactionConfirmationWatchingRule<TCallbackResult>>> ListAsync(CancellationToken cancellationToken)
        {
            using (var db = this.db.CreateDbContext())
            {
                var watches = await db.TransactionConfirmationWatchingRules
                    .Include(e => e.Callback)
                    .ToListAsync(cancellationToken);

                return watches.Select(e => ToDomain(e));
            }
        }

        public async Task SubtractRemainingWaitingTimeAsync(Guid id, TimeSpan consumedTime, CancellationToken cancellationToken)
        {
            using (var db = this.db.CreateDbContext())
            {
                var watch = await db.TransactionConfirmationWatchingRules
                    .Include(e => e.Callback)
                    .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

                if (watch == null)
                {
                    throw new KeyNotFoundException("Watch id not found.");
                }

                if (consumedTime < TimeSpan.Zero)
                {
                    throw new ArgumentException("Consumed time could not be negative.");
                }

                watch.RemainingWaitingTime -= consumedTime;
                watch.RemainingWaitingTime = watch.RemainingWaitingTime < TimeSpan.Zero
                    ? TimeSpan.Zero
                    : watch.RemainingWaitingTime;

                await db.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task UpdateStatusAsync(Guid id, TransactionConfirmationWatchingRuleStatus status, CancellationToken cancellationToken)
        {
            using (var db = this.db.CreateDbContext())
            {
                var watch = await db.TransactionConfirmationWatchingRules
                    .Include(e => e.Callback)
                    .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

                if (watch == null)
                {
                    throw new KeyNotFoundException("Watch id not found");
                }

                watch.Status = (int)status;

                await db.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task<TimeSpan> GetRemainingWaitingTimeAsync(Guid id, CancellationToken cancellationToken)
        {
            using (var db = this.db.CreateDbContext())
            {
                var watch = await db.TransactionConfirmationWatchingRules
                    .Include(e => e.Callback)
                    .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

                if (watch == null)
                {
                    throw new KeyNotFoundException("Watch id not found.");
                }

                return watch.RemainingWaitingTime;
            }
        }

        public static TransactionConfirmationWatchingRule<TCallbackResult> ToDomain(
            Ztm.Data.Entity.Contexts.Main.TransactionConfirmationWatchingRule watch,
            Callback callback = null)
        {
            return new TransactionConfirmationWatchingRule<TCallbackResult>(
                watch.Id,
                watch.Transaction,
                (TransactionConfirmationWatchingRuleStatus)watch.Status,
                watch.Confirmation,
                watch.WaitingTime,
                JsonConvert.DeserializeObject<TCallbackResult>(watch.SuccessData),
                JsonConvert.DeserializeObject<TCallbackResult>(watch.TimeoutData),
                callback != null
                    ? callback
                    : (watch.Callback == null ? null : SqlCallbackRepository.ToDomain(watch.Callback))
            );
        }
    }
}