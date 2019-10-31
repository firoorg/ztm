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
            uint256 transaction, int confirmation, TimeSpan timeout, TCallbackResult successData, TCallbackResult timeoutData, Callback callback, CancellationToken cancellationToken)
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
                        Confirmation = confirmation,
                        Due = DateTime.UtcNow.Add(timeout),
                        SuccessData = JsonConvert.SerializeObject(successData),
                        TimeoutData = JsonConvert.SerializeObject(timeoutData),
                    }, cancellationToken);

                await db.SaveChangesAsync(cancellationToken);

                return ToDomain(watch.Entity);
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

        static TransactionConfirmationWatch<TCallbackResult> ToDomain(Ztm.Data.Entity.Contexts.Main.TransactionConfirmationWatch watch)
        {
            return new TransactionConfirmationWatch<TCallbackResult>(
                watch.Id,
                watch.Transaction,
                watch.Confirmation,
                DateTime.SpecifyKind(watch.Due, DateTimeKind.Utc),
                JsonConvert.DeserializeObject<TCallbackResult>(watch.SuccessData),
                JsonConvert.DeserializeObject<TCallbackResult>(watch.TimeoutData),
                watch.Callback == null ? null : SqlCallbackRepository.ToDomain(watch.Callback)
            );
        }
    }
}