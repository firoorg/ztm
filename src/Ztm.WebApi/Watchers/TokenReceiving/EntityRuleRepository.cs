using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Ztm.Data.Entity.Contexts;
using Ztm.WebApi.AddressPools;
using Ztm.WebApi.Callbacks;
using Ztm.Zcoin.NBitcoin.Exodus;
using EntityModel = Ztm.Data.Entity.Contexts.Main.TokenReceivingWatcherRule;
using Status = Ztm.Data.Entity.Contexts.Main.TokenReceivingWatcherRuleStatus;

namespace Ztm.WebApi.Watchers.TokenReceiving
{
    public sealed class EntityRuleRepository : IRuleRepository
    {
        readonly IMainDatabaseFactory db;
        readonly ICallbackRepository callbacks;
        readonly IReceivingAddressRepository addresses;

        public EntityRuleRepository(
            IMainDatabaseFactory db,
            ICallbackRepository callbacks,
            IReceivingAddressRepository addresses)
        {
            if (db == null)
            {
                throw new ArgumentNullException(nameof(db));
            }

            if (callbacks == null)
            {
                throw new ArgumentNullException(nameof(callbacks));
            }

            if (addresses == null)
            {
                throw new ArgumentNullException(nameof(addresses));
            }

            this.db = db;
            this.callbacks = callbacks;
            this.addresses = addresses;
        }

        public async Task AddAsync(Rule rule, CancellationToken cancellationToken)
        {
            if (rule == null)
            {
                throw new ArgumentNullException(nameof(rule));
            }

            using (var db = this.db.CreateDbContext())
            {
                var entity = new EntityModel()
                {
                    Id = rule.Id,
                    CallbackId = rule.Callback.Id,
                    PropertyId = rule.Property.Value,
                    AddressReservationId = rule.AddressReservation.Id,
                    TargetAmount = rule.TargetAmount.Indivisible,
                    TargetConfirmation = rule.TargetConfirmation,
                    OriginalTimeout = rule.OriginalTimeout,
                    CurrentTimeout = rule.OriginalTimeout,
                    TimeoutStatus = rule.TimeoutStatus,
                    Status = Status.Uncompleted,
                };

                await db.TokenReceivingWatcherRules.AddAsync(entity, cancellationToken);
                await db.SaveChangesAsync();
            }
        }

        public async Task DecreaseTimeoutAsync(Guid id, TimeSpan value, CancellationToken cancellationToken)
        {
            using (var db = this.db.CreateDbContext())
            {
                await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);

                var entity = await db.TokenReceivingWatcherRules
                    .SingleOrDefaultAsync(e => e.Id == id, cancellationToken);

                if (entity == null)
                {
                    throw new ArgumentException("The value is not a valid identifier.", nameof(id));
                }

                entity.CurrentTimeout -= value;

                if (entity.CurrentTimeout < TimeSpan.Zero)
                {
                    entity.CurrentTimeout = TimeSpan.Zero;
                }

                await db.SaveChangesAsync(cancellationToken);
                db.Database.CommitTransaction();
            }
        }

        public async Task<Rule> GetAsync(Guid id, CancellationToken cancellationToken)
        {
            using (var db = this.db.CreateDbContext())
            {
                var entity = await db.TokenReceivingWatcherRules
                    .SingleOrDefaultAsync(e => e.Id == id, cancellationToken);

                return entity != null ? await ToDomainAsync(entity, cancellationToken) : null;
            }
        }

        public async Task<TimeSpan> GetCurrentTimeoutAsync(Guid id, CancellationToken cancellationToken)
        {
            using (var db = this.db.CreateDbContext())
            {
                var entity = await db.TokenReceivingWatcherRules.SingleOrDefaultAsync(e => e.Id == id, cancellationToken);

                if (entity == null)
                {
                    throw new ArgumentException("The value is not a valid identifier.", nameof(id));
                }

                return entity.CurrentTimeout;
            }
        }

        public async Task<IEnumerable<Rule>> ListUncompletedAsync(
            PropertyId property,
            CancellationToken cancellationToken)
        {
            IEnumerable<EntityModel> entities;

            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            using (var db = this.db.CreateDbContext())
            {
                entities = await db.TokenReceivingWatcherRules
                    .Where(e => e.Status == Status.Uncompleted && e.PropertyId == property.Value)
                    .ToListAsync(cancellationToken);
            }

            return await Task.WhenAll(entities.Select(e => ToDomainAsync(e, cancellationToken)));
        }

        public async Task SetSucceededAsync(Guid id, CancellationToken cancellationToken)
        {
            using (var db = this.db.CreateDbContext())
            {
                await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);

                var entity = await db.TokenReceivingWatcherRules
                    .SingleOrDefaultAsync(e => e.Id == id, cancellationToken);

                if (entity == null)
                {
                    throw new ArgumentException("The value is not a valid identifier.", nameof(id));
                }

                entity.Status = Status.Succeeded;

                await db.SaveChangesAsync(cancellationToken);
                db.Database.CommitTransaction();
            }
        }

        public async Task SetTimedOutAsync(Guid id, CancellationToken cancellationToken)
        {
            using (var db = this.db.CreateDbContext())
            {
                await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);

                var entity = await db.TokenReceivingWatcherRules
                    .SingleOrDefaultAsync(e => e.Id == id, cancellationToken);

                if (entity == null)
                {
                    throw new ArgumentException("The value is not a valid identifier.", nameof(id));
                }

                entity.Status = Status.TimedOut;

                await db.SaveChangesAsync(cancellationToken);
                db.Database.CommitTransaction();
            }
        }

        async Task<Rule> ToDomainAsync(EntityModel entity, CancellationToken cancellationToken)
        {
            var reservation = this.addresses.GetReservationAsync(entity.AddressReservationId, cancellationToken);
            var callback = this.callbacks.GetAsync(entity.CallbackId, cancellationToken);

            await Task.WhenAll(reservation, callback);

            return new Rule(
                new PropertyId(entity.PropertyId),
                reservation.Result,
                new PropertyAmount(entity.TargetAmount),
                entity.TargetConfirmation,
                entity.OriginalTimeout,
                entity.TimeoutStatus,
                callback.Result,
                entity.Id);
        }
    }
}
