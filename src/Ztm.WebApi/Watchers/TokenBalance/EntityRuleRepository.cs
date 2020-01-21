using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NBitcoin;
using Ztm.Data.Entity.Contexts;
using Ztm.Zcoin.NBitcoin.Exodus;
using EntityModel = Ztm.Data.Entity.Contexts.Main.TokenBalanceWatcherRule;
using Status = Ztm.Data.Entity.Contexts.Main.TokenBalanceWatcherRuleStatus;

namespace Ztm.WebApi.Watchers.TokenBalance
{
    public sealed class EntityRuleRepository : IRuleRepository
    {
        readonly IMainDatabaseFactory db;
        readonly Network network;

        public EntityRuleRepository(IMainDatabaseFactory db, Network network)
        {
            if (db == null)
            {
                throw new ArgumentNullException(nameof(db));
            }

            if (network == null)
            {
                throw new ArgumentNullException(nameof(network));
            }

            this.db = db;
            this.network = network;
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
                    CallbackId = rule.Callback,
                    PropertyId = rule.Property.Value,
                    Address = rule.Address.ToString(),
                    TargetAmount = rule.TargetAmount.Indivisible,
                    TargetConfirmation = rule.TargetConfirmation,
                    OriginalTimeout = rule.OriginalTimeout,
                    CurrentTimeout = rule.OriginalTimeout,
                    TimeoutStatus = rule.TimeoutStatus,
                    Status = Status.Uncompleted,
                };

                await db.TokenBalanceWatcherRules.AddAsync(entity, cancellationToken);
                await db.SaveChangesAsync();
            }
        }

        public async Task DecreaseTimeoutAsync(Guid id, TimeSpan value, CancellationToken cancellationToken)
        {
            using (var db = this.db.CreateDbContext())
            {
                await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);

                var entity = await db.TokenBalanceWatcherRules.SingleOrDefaultAsync(e => e.Id == id, cancellationToken);

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

        public async Task<TimeSpan> GetCurrentTimeoutAsync(Guid id, CancellationToken cancellationToken)
        {
            using (var db = this.db.CreateDbContext())
            {
                var entity = await db.TokenBalanceWatcherRules.SingleOrDefaultAsync(e => e.Id == id, cancellationToken);

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
                entities = await db.TokenBalanceWatcherRules
                    .Where(e => e.Status == Status.Uncompleted && e.PropertyId == property.Value)
                    .ToListAsync(cancellationToken);
            }

            return entities.Select(e => ToDomain(e)).ToList();
        }

        public async Task SetSucceededAsync(Guid id, CancellationToken cancellationToken)
        {
            using (var db = this.db.CreateDbContext())
            {
                await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);

                var entity = await db.TokenBalanceWatcherRules.SingleOrDefaultAsync(e => e.Id == id, cancellationToken);

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

                var entity = await db.TokenBalanceWatcherRules.SingleOrDefaultAsync(e => e.Id == id, cancellationToken);

                if (entity == null)
                {
                    throw new ArgumentException("The value is not a valid identifier.", nameof(id));
                }

                entity.Status = Status.TimedOut;

                await db.SaveChangesAsync(cancellationToken);
                db.Database.CommitTransaction();
            }
        }

        public Rule ToDomain(EntityModel entity)
        {
            return new Rule(
                new PropertyId(entity.PropertyId),
                BitcoinAddress.Create(entity.Address, this.network),
                new PropertyAmount(entity.TargetAmount),
                entity.TargetConfirmation,
                entity.OriginalTimeout,
                entity.TimeoutStatus,
                entity.CallbackId,
                entity.Id);
        }
    }
}
