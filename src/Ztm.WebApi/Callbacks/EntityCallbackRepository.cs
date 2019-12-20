using System;
using System.Data;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Ztm.Data.Entity.Contexts;
using Ztm.Data.Entity.Contexts.Main;

namespace Ztm.WebApi.Callbacks
{
    public class EntityCallbackRepository : ICallbackRepository
    {
        readonly IMainDatabaseFactory db;

        public EntityCallbackRepository(IMainDatabaseFactory db)
        {
            if (db == null)
            {
                throw new ArgumentNullException(nameof(db));
            }

            this.db = db;
        }

        public static Callback ToDomain(WebApiCallback callback)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            return new Callback(
                callback.Id,
                callback.RegisteredIp,
                DateTime.SpecifyKind(callback.RegisteredTime, DateTimeKind.Utc),
                callback.Completed,
                callback.Url
            );
        }

        public async Task<Callback> AddAsync(IPAddress registeringIp, Uri url, CancellationToken cancellationToken)
        {
            if (registeringIp == null)
            {
                throw new ArgumentNullException(nameof(registeringIp));
            }

            if (url == null)
            {
                throw new ArgumentNullException(nameof(url));
            }

            using (var db = this.db.CreateDbContext())
            {
                var callback = await db.WebApiCallbacks.AddAsync(new WebApiCallback()
                {
                    Id = Guid.NewGuid(),
                    RegisteredIp = registeringIp,
                    RegisteredTime = DateTime.UtcNow,
                    Url = url,
                }, cancellationToken);

                await db.SaveChangesAsync(cancellationToken);

                return ToDomain(callback.Entity);
            }
        }

        public async Task SetCompletedAsyc(Guid id, CancellationToken cancellationToken)
        {
            using (var db = this.db.CreateDbContext())
            using (var dbtx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken))
            {
                var update = await db.WebApiCallbacks.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

                if (update != null)
                {
                    update.Completed = true;

                    await db.SaveChangesAsync();
                    dbtx.Commit();
                }
            }
        }

        public async Task<Callback> GetAsync(Guid id, CancellationToken cancellationToken)
        {
            using (var db = this.db.CreateDbContext())
            {
                var callback = await db.WebApiCallbacks.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
                return callback == null ? null : ToDomain(callback);
            }
        }

        public async Task AddHistoryAsync(Guid id, CallbackResult result, CancellationToken cancellationToken)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            using (var db = this.db.CreateDbContext())
            {
                await db.WebApiCallbackHistories.AddAsync
                (
                    new WebApiCallbackHistory
                    {
                        CallbackId = id,
                        Status = result.Status,
                        Data = JsonConvert.SerializeObject(result.Data),
                        InvokedTime = DateTime.UtcNow,
                    }
                );
                await db.SaveChangesAsync(cancellationToken);
            }
        }
    }
}