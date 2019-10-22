using System;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Ztm.Data.Entity.Contexts;
using Ztm.Data.Entity.Contexts.Main;

namespace Ztm.WebApi
{
    public class SqlCallbackRepository : ICallbackRepository
    {
        readonly IMainDatabaseFactory db;

        public SqlCallbackRepository(IMainDatabaseFactory db)
        {
            if (db == null)
            {
                throw new ArgumentNullException(nameof(db));
            }

            this.db = db;
        }

        public async Task<Callback> AddAsync(IPAddress ip, Uri url, CancellationToken cancellationToken)
        {
            if (ip == null)
            {
                throw new ArgumentNullException(nameof(ip));
            }

            if (url == null)
            {
                throw new ArgumentNullException(nameof(url));
            }

            using (var db = this.db.CreateDbContext())
            {
                var callback = await db.WebApiCallbacks.AddAsync(new WebApiCallback()
                {
                    RegisteredIp = ip,
                    RegisteredTime = DateTime.Now.ToUniversalTime(),
                    Url = url,
                }, cancellationToken);

                var webApiCallback = (WebApiCallback)callback.Entity;

                await db.SaveChangesAsync(cancellationToken);

                return ToDomain(webApiCallback);
            }
        }

        public async Task SetCompletedAsyc(Guid id, CancellationToken cancellationToken)
        {
            using (var db = this.db.CreateDbContext())
            using (var dbtx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken))
            {
                var update = await db.WebApiCallbacks.FindAsync(new object[]{ id }, cancellationToken);

                if (update == null)
                {
                    throw new KeyNotFoundException($"Id {id} is not found");
                }

                update.Completed = true;

                await db.SaveChangesAsync();
                dbtx.Commit();
            }
        }

        public async Task<Callback> GetAsync(Guid id, CancellationToken cancellationToken)
        {
            using (var db = this.db.CreateDbContext())
            {
                var callback = await db.WebApiCallbacks.FindAsync(new object[]{ id }, cancellationToken);
                return callback == null ? null : ToDomain(callback);
            }
        }

        public async Task AddHistoryAsync(Guid id, string status, string data, CancellationToken cancellationToken)
        {
            if (status == null)
            {
                throw new ArgumentNullException(nameof(status));
            }

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            using (var db = this.db.CreateDbContext())
            using (var dbtx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken))
            {
                var callback = await db.WebApiCallbacks.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);

                if (callback == null)
                {
                    throw new KeyNotFoundException($"Id {id} is not found");
                }

                await db.WebApiCallbackHistories.AddAsync(
                    new WebApiCallbackHistory{
                        CallbackId = id,
                        Status = status,
                        Data = data,
                        InvokedTime = DateTime.Now.ToUniversalTime(),
                    }
                );
                await db.SaveChangesAsync(cancellationToken);
                dbtx.Commit();
            }
        }

        static WebApiCallback ToEntity(Callback callback)
        {
            return new WebApiCallback
            {
                Id = callback.Id,
                RegisteredIp = callback.RegisteredIp,
                RegisteredTime = callback.RegisteredTime.ToUniversalTime(),
                Completed = callback.Completed,
                Url = callback.Url,
            };
        }

        static Callback ToDomain(WebApiCallback callback)
        {
            return new Callback(
                callback.Id,
                callback.RegisteredIp,
                DateTime.SpecifyKind(callback.RegisteredTime, DateTimeKind.Utc),
                callback.Completed,
                callback.Url
            );
        }
    }
}