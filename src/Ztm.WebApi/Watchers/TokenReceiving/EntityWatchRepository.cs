using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NBitcoin;
using Ztm.Data.Entity.Contexts;
using Ztm.Zcoin.NBitcoin.Exodus;
using DomainModel = Ztm.Zcoin.Watching.BalanceWatch<Ztm.WebApi.Watchers.TokenReceiving.Rule, Ztm.Zcoin.NBitcoin.Exodus.PropertyAmount>;
using EntityModel = Ztm.Data.Entity.Contexts.Main.TokenReceivingWatcherWatch;
using Status = Ztm.Data.Entity.Contexts.Main.TokenReceivingWatcherWatchStatus;

namespace Ztm.WebApi.Watchers.TokenReceiving
{
    public sealed class EntityWatchRepository : IWatchRepository
    {
        readonly IMainDatabaseFactory db;
        readonly IRuleRepository rules;

        public EntityWatchRepository(IMainDatabaseFactory db, IRuleRepository rules)
        {
            if (db == null)
            {
                throw new ArgumentNullException(nameof(db));
            }

            if (rules == null)
            {
                throw new ArgumentNullException(nameof(rules));
            }

            this.db = db;
            this.rules = rules;
        }

        public async Task AddAsync(IEnumerable<DomainModel> watches, CancellationToken cancellationToken)
        {
            if (watches == null)
            {
                throw new ArgumentNullException(nameof(watches));
            }

            var entities = watches
                .Select(w => ToEntity(w))
                .ToList();

            using (var db = this.db.CreateDbContext())
            {
                await db.TokenReceivingWatcherWatches.AddRangeAsync(entities, cancellationToken);
                await db.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task<IEnumerable<DomainModel>> ListUncompletedAsync(
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
                entities = await db.TokenReceivingWatcherWatches
                    .Where(e => e.Status == Status.Uncompleted && e.Rule.PropertyId == property.Value)
                    .ToListAsync(cancellationToken);
            }

            return await Task.WhenAll(entities.Select(e => ToDomainAsync(e, cancellationToken)));
        }

        public Task SetConfirmationCountAsync(
            IReadOnlyDictionary<DomainModel, int> watches,
            CancellationToken cancellationToken)
        {
            if (watches == null)
            {
                throw new ArgumentNullException(nameof(watches));
            }

            var index = watches.ToDictionary(p => p.Key.Id, p => p.Key);

            return UpdateAsync(
                e => index.Keys.Contains(e.Id),
                l =>
                {
                    var ids = l
                        .Select(e => e.Id)
                        .ToHashSet();

                    var invalid = watches
                        .Where(p => !ids.Contains(p.Key.Id))
                        .Select(p => p.Key.Id)
                        .ToList();

                    if (invalid.Count != 0)
                    {
                        var ex = new ArgumentException("Some of watches does not exists.", nameof(watches));
                        ex.Data.Add("Identifiers", invalid);
                        throw ex;
                    }
                },
                e => e.Confirmation = watches[index[e.Id]],
                cancellationToken);
        }

        public Task<IReadOnlyDictionary<DomainModel, int>> TransitionToRejectedAsync(
            PropertyId property,
            uint256 startBlock,
            CancellationToken cancellationToken)
        {
            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            if (startBlock == null)
            {
                throw new ArgumentNullException(nameof(startBlock));
            }

            return CompleteAsync(
                e => e.Status == Status.Uncompleted && e.Rule.PropertyId == property.Value && e.BlockId == startBlock,
                Status.Rejected,
                cancellationToken);
        }

        public Task<IReadOnlyDictionary<DomainModel, int>> TransitionToSucceededAsync(
            IEnumerable<DomainModel> watches,
            CancellationToken cancellationToken)
        {
            if (watches == null)
            {
                throw new ArgumentNullException(nameof(watches));
            }

            var target = watches
                .Select(w => w.Id)
                .ToList();

            return CompleteAsync(
                e => e.Status == Status.Uncompleted && target.Contains(e.Id),
                Status.Succeeded,
                cancellationToken);
        }

        public Task<IReadOnlyDictionary<DomainModel, int>> TransitionToTimedOutAsync(
            Rule rule,
            CancellationToken cancellationToken)
        {
            if (rule == null)
            {
                throw new ArgumentNullException(nameof(rule));
            }

            return CompleteAsync(
                e => e.Status == Status.Uncompleted && e.RuleId == rule.Id,
                Status.TimedOut,
                cancellationToken);
        }

        static EntityModel ToEntity(DomainModel domain)
        {
            return new EntityModel()
            {
                Id = domain.Id,
                RuleId = domain.Context.Id,
                BlockId = domain.StartBlock,
                TransactionId = domain.Transaction,
                BalanceChange = domain.BalanceChange.Indivisible,
                CreatedTime = domain.StartTime.ToUniversalTime(),
                Confirmation = 0,
                Status = Status.Uncompleted,
            };
        }

        async Task<IReadOnlyDictionary<DomainModel, int>> CompleteAsync(
            Expression<Func<EntityModel, bool>> criteria,
            Status status,
            CancellationToken cancellationToken)
        {
            var entities = await UpdateAsync(criteria, e => e.Status = status, cancellationToken);
            var completed = new Dictionary<DomainModel, int>();

            foreach (var entity in entities)
            {
                var domain = await ToDomainAsync(entity, CancellationToken.None);
                completed.Add(domain, entity.Confirmation);
            }

            return completed;
        }

        Task<IEnumerable<EntityModel>> UpdateAsync(
            Expression<Func<EntityModel, bool>> criteria,
            Action<EntityModel> updater,
            CancellationToken cancellationToken)
        {
            return UpdateAsync(criteria, null, updater, cancellationToken);
        }

        async Task<IEnumerable<EntityModel>> UpdateAsync(
            Expression<Func<EntityModel, bool>> criteria,
            Action<IEnumerable<EntityModel>> verifier,
            Action<EntityModel> updater,
            CancellationToken cancellationToken)
        {
            IEnumerable<EntityModel> entities;

            using (var db = this.db.CreateDbContext())
            {
                await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);

                // Load entities to update.
                entities = await db.TokenReceivingWatcherWatches
                    .Where(criteria)
                    .ToListAsync(cancellationToken);

                if (verifier != null)
                {
                    verifier(entities);
                }

                // Update all matched entities.
                foreach (var entity in entities)
                {
                    updater(entity);
                }

                // Commit.
                await db.SaveChangesAsync(cancellationToken);
                db.Database.CommitTransaction();
            }

            return entities;
        }

        async Task<DomainModel> ToDomainAsync(EntityModel entity, CancellationToken cancellationToken)
        {
            var rule = await this.rules.GetAsync(entity.RuleId, cancellationToken);

            return new DomainModel(
                rule,
                entity.BlockId,
                entity.TransactionId,
                rule.AddressReservation.Address.Address,
                new PropertyAmount(entity.BalanceChange),
                DateTime.SpecifyKind(entity.CreatedTime, DateTimeKind.Utc).ToLocalTime(),
                entity.Id);
        }
    }
}
