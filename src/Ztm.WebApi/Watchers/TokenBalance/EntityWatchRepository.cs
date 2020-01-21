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
using DomainModel = Ztm.Zcoin.Watching.BalanceWatch<Ztm.WebApi.Watchers.TokenBalance.Rule, Ztm.Zcoin.NBitcoin.Exodus.PropertyAmount>;
using EntityModel = Ztm.Data.Entity.Contexts.Main.TokenBalanceWatcherWatch;
using Status = Ztm.Data.Entity.Contexts.Main.TokenBalanceWatcherWatchStatus;

namespace Ztm.WebApi.Watchers.TokenBalance
{
    public sealed class EntityWatchRepository : IWatchRepository
    {
        readonly IMainDatabaseFactory db;
        readonly EntityRuleRepository rules;

        public EntityWatchRepository(IMainDatabaseFactory db, EntityRuleRepository rules)
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

            using (var db = this.db.CreateDbContext())
            {
                var entities = watches
                    .Select(w => ToEntity(w))
                    .ToList();

                await db.TokenBalanceWatcherWatches.AddRangeAsync(entities, cancellationToken);
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
                entities = await db.TokenBalanceWatcherWatches
                    .Include(e => e.Rule)
                    .Where(e => e.Status == Status.Uncompleted && e.Rule.PropertyId == property.Value)
                    .ToListAsync(cancellationToken);
            }

            return entities
                .Select(e => ToDomain(e))
                .ToList();
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
                    var invalid = watches
                        .Where(p => !l.Any(e => e.Id == p.Key.Id))
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

        public Task<IEnumerable<CompletedWatch>> TransitionToRejectedAsync(
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

        public Task<IEnumerable<CompletedWatch>> TransitionToSucceededAsync(
            IEnumerable<DomainModel> watches,
            CancellationToken cancellationToken)
        {
            if (watches == null)
            {
                throw new ArgumentNullException(nameof(watches));
            }

            var target = watches.Select(w => w.Id).ToList();

            return CompleteAsync(
                e => e.Status == Status.Uncompleted && target.Contains(e.Id),
                Status.Succeeded,
                cancellationToken);
        }

        public Task<IEnumerable<CompletedWatch>> TransitionToTimedOutAsync(
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

        async Task<IEnumerable<CompletedWatch>> CompleteAsync(
            Expression<Func<EntityModel, bool>> criteria,
            Status status,
            CancellationToken cancellationToken)
        {
            var entities = await UpdateAsync(criteria, e => e.Status = status, cancellationToken);

            return entities
                .Select(e => new CompletedWatch(ToDomain(e), e.Confirmation))
                .ToList();
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
                entities = await db.TokenBalanceWatcherWatches
                    .Include(e => e.Rule)
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

        DomainModel ToDomain(EntityModel entity)
        {
            var rule = this.rules.ToDomain(entity.Rule);

            return new DomainModel(
                rule,
                entity.BlockId,
                entity.TransactionId,
                rule.Address,
                new PropertyAmount(entity.BalanceChange),
                DateTime.SpecifyKind(entity.CreatedTime, DateTimeKind.Utc).ToLocalTime(),
                entity.Id);
        }
    }
}
