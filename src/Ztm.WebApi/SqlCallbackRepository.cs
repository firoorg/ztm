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
            this.db = db;
        }

        public async Task<Callback> AddAsync(IPAddress ip, DateTime requestTime, Uri url, CancellationToken cancellationToken)
        {
            if (ip == null)
            {
                throw new ArgumentNullException(nameof(ip));
            }

            if (url == null)
            {
                throw new ArgumentNullException(nameof(url));
            }

            var callback = new Callback(Guid.NewGuid(), ip, requestTime, false, url);

            using (var db = this.db.CreateDbContext())
            using (var dbtx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken))
            {
                await db.WebApiCallbacks.AddAsync(ToEntity(callback));
                await db.SaveChangesAsync(cancellationToken);

                var webApiCallback = await db.WebApiCallbacks.FirstOrDefaultAsync(c => c.Id == callback.Id);
                dbtx.Commit();

                return ToDomain(webApiCallback);
            }
        }

        public async Task<Callback> SetStatusAsCompletedAsync(Guid id, CancellationToken cancellationToken)
        {
            using (var db = this.db.CreateDbContext())
            using (var dbtx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken))
            {
                var update = await db.WebApiCallbacks.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

                if (update == null)
                {
                    throw new KeyNotFoundException($"Id {id} is not found");
                }

                update.Completed = true;

                await db.SaveChangesAsync();
                dbtx.Commit();

                return ToDomain(update);
            }
        }

        public async Task<Callback> GetAsync(Guid id, CancellationToken cancellationToken)
        {
            using (var db = this.db.CreateDbContext())
            {
                var callback = await db.WebApiCallbacks.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);

                return callback == null ? null : ToDomain(callback);
            }
        }

        public async Task AddInvocationAsync(Guid id, string status, DateTime invokedTime, byte[] data, CancellationToken cancellationToken)
        {
            if (status == null)
            {
                throw new ArgumentNullException(nameof(status));
            }

            using (var db = this.db.CreateDbContext())
            using (var dbtx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken))
            {
                var callback = await db.WebApiCallbacks.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);

                if (callback == null)
                {
                    throw new KeyNotFoundException($"Id {id} is not found");
                }

                var invocation = new WebApiCallbackHistory
                {
                    CallbackId = id,
                    Status = status,
                    InvokedTime = invokedTime.ToUniversalTime(),
                    Data = data,
                };

                await db.WebApiCallbackHistories.AddAsync(invocation);
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