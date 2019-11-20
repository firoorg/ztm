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
    public class SqlTransactionConfirmationWatchRepository<TCallbackResult> : ITransactionConfirmationWatchRepository<TCallbackResult>
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

        public async Task<TransactionConfirmationWatch<TCallbackResult>> AddAsync(
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
                var watch = await db.TransactionConfirmationWatches
                    .AddAsync(
                    new Ztm.Data.Entity.Contexts.Main.TransactionConfirmationWatch
                    {
                        Id = Guid.NewGuid(),
                        CallbackId = callback.Id,
                        Transaction = transaction,
                        Completed = false,
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

        public async Task<TransactionConfirmationWatch<TCallbackResult>> GetAsync(Guid id, CancellationToken cancellationToken)
        {
            using (var db = this.db.CreateDbContext())
            {
                var watch = await db.TransactionConfirmationWatches
                    .Include(e => e.Callback)
                    .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

                return watch == null ? null : ToDomain(watch);
            }
        }

        public async Task<IEnumerable<TransactionConfirmationWatch<TCallbackResult>>> ListAsync(CancellationToken cancellationToken)
        {
            using (var db = this.db.CreateDbContext())
            {
                var watches = await db.TransactionConfirmationWatches
                    .Include(e => e.Callback)
                    .ToListAsync(cancellationToken);

                return watches.Select(e => ToDomain(e));
            }
        }

        public async Task SetRemainingWaitingTimeAsync(Guid id, TimeSpan remainingTime, CancellationToken cancellationToken)
        {
            using (var db = this.db.CreateDbContext())
            {
                var watch = await db.TransactionConfirmationWatches
                    .Include(e => e.Callback)
                    .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

                if (watch == null)
                {
                    throw new KeyNotFoundException("Watch id not found");
                }

                if (remainingTime > watch.RemainingWaitingTime)
                {
                    throw new ArgumentException("Remaining time must be not less than current remaining time");
                }

                watch.RemainingWaitingTime = remainingTime;

                await db.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task CompleteAsync(Guid id, CancellationToken cancellationToken)
        {
            using (var db = this.db.CreateDbContext())
            {
                var watch = await db.TransactionConfirmationWatches
                    .Include(e => e.Callback)
                    .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

                if (watch == null)
                {
                    throw new KeyNotFoundException("Watch id not found");
                }

                watch.Completed = true;

                await db.SaveChangesAsync(cancellationToken);
            }
        }

        static TransactionConfirmationWatch<TCallbackResult> ToDomain(
            Ztm.Data.Entity.Contexts.Main.TransactionConfirmationWatch watch,
            Callback callback = null)
        {
            return new TransactionConfirmationWatch<TCallbackResult>(
                watch.Id,
                watch.Transaction,
                watch.Completed,
                watch.Confirmation,
                watch.WaitingTime,
                watch.RemainingWaitingTime,
                JsonConvert.DeserializeObject<TCallbackResult>(watch.SuccessData),
                JsonConvert.DeserializeObject<TCallbackResult>(watch.TimeoutData),
                callback != null
                    ? callback
                    : watch.Callback == null ? null : SqlCallbackRepository.ToDomain(watch.Callback)
            );
        }
    }
}