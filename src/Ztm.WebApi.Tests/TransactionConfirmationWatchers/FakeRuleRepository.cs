using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using Ztm.WebApi.Callbacks;
using Ztm.WebApi.TransactionConfirmationWatchers;

namespace Ztm.WebApi.Tests.TransactionConfirmationWatchers
{
    public class FakeRuleRepository : IRuleRepository
    {
        readonly Dictionary<Guid, RuleWithAdditionalDatas> rules;

        public FakeRuleRepository()
        {
            this.rules = new Dictionary<Guid, RuleWithAdditionalDatas>();
        }

        public virtual Task<Rule> AddAsync(uint256 transaction, int confirmation, TimeSpan unconfirmedWaitingTime,
            CallbackResult successResponse, CallbackResult timeoutResponse, Callback callback, CancellationToken cancellationToken)
        {
            var id = Guid.NewGuid();

            var rule = new Rule(id, transaction, confirmation, unconfirmedWaitingTime, successResponse, timeoutResponse,
                callback, null);

            this.rules.Add(id, new RuleWithAdditionalDatas
            {
                Rule = rule,
                RemainingTime = unconfirmedWaitingTime,
                Status = RuleStatus.Pending,
            });

            return Task.FromResult(rule);
        }

        public virtual Task ClearCurrentWatchIdAsync(Guid id, CancellationToken cancellationToken)
        {
            this.update(id,
                (old) =>
                {
                    var builder = new RuleBuilder(old.Rule);
                    builder.CurrentWatchId = null;

                    return new RuleWithAdditionalDatas
                    {
                        Rule = builder.Build(),
                        RemainingTime = old.RemainingTime,
                        Status = old.Status
                    };
                }
            );

            return Task.CompletedTask;
        }

        public virtual Task<Rule> GetAsync(Guid id, CancellationToken cancellationToken)
        {
            if (this.rules.TryGetValue(id, out var data))
            {
                return Task.FromResult(data.Rule);
            }

            throw new KeyNotFoundException();
        }

        public virtual Task<TimeSpan> GetRemainingWaitingTimeAsync(Guid id, CancellationToken cancellationToken)
        {
            if (this.rules.TryGetValue(id, out var data))
            {
                return Task.FromResult(data.RemainingTime);
            }

            throw new KeyNotFoundException();
        }

        public virtual Task<RuleStatus> GetStatusAsync(Guid id, CancellationToken cancellationToken)
        {
            if (this.rules.TryGetValue(id, out var data))
            {
                return Task.FromResult(data.Status);
            }

            throw new KeyNotFoundException();
        }

        public virtual Task<IEnumerable<Rule>> ListActiveAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(this.rules.Where(r => r.Value.Status == RuleStatus.Pending).Select(r => r.Value.Rule).AsEnumerable());
        }

        public virtual Task SubtractRemainingWaitingTimeAsync(Guid id, TimeSpan remainingTime, CancellationToken cancellationToken)
        {
            this.update(id,
                (old) =>
                {
                    old.RemainingTime -= remainingTime;
                    old.RemainingTime = old.RemainingTime < TimeSpan.Zero ? TimeSpan.Zero : old.RemainingTime;
                    return old;
                }
            );

            return Task.CompletedTask;
        }

        public virtual Task UpdateCurrentWatchIdAsync(Guid id, Guid watchId, CancellationToken cancellationToken)
        {
            this.update(id,
                (old) =>
                {
                    var builder = new RuleBuilder(old.Rule);
                    builder.CurrentWatchId = watchId;

                    return new RuleWithAdditionalDatas
                    {
                        Rule = builder.Build(),
                        RemainingTime = old.RemainingTime,
                        Status = old.Status
                    };
                }
            );

            return Task.CompletedTask;
        }

        public virtual Task UpdateStatusAsync(Guid id, RuleStatus status, CancellationToken cancellationToken)
        {
            this.update(id,
                (old) =>
                {
                    return new RuleWithAdditionalDatas
                    {
                        Rule = old.Rule,
                        RemainingTime = old.RemainingTime,
                        Status = status
                    };
                }
            );

            return Task.CompletedTask;
        }

        RuleWithAdditionalDatas update(Guid id, Func<RuleWithAdditionalDatas, RuleWithAdditionalDatas> update)
        {
            if (!this.rules.TryGetValue(id, out var old))
            {
                throw new KeyNotFoundException();
            }
            rules.Remove(id);

            var updated = update(old);
            rules.Add(id, updated);

            return updated;
        }
    }

    class RuleWithAdditionalDatas
    {
        public Rule Rule { get; set; }
        public TimeSpan RemainingTime { get; set; }
        public RuleStatus Status { get; set; }
    }

    class RuleBuilder
    {
        public Guid Id { get; set; }
        public uint256 Transaction { get; set; }
        public int Confirmations { get; set; }
        public TimeSpan WaitingTime { get; set; }
        public dynamic SuccessResponse { get; set; }
        public dynamic TimeoutResponse { get; set; }
        public Callback Callback { get; set; }
        public Guid? CurrentWatchId { get; set; }

        public RuleBuilder(Rule old)
        {
            Id = old.Id;
            Transaction = old.Transaction;
            Confirmations = old.Confirmations;
            WaitingTime = old.WaitingTime;
            SuccessResponse = old.SuccessResponse;
            TimeoutResponse = old.TimeoutResponse;
            Callback = old.Callback;
            CurrentWatchId = old.CurrentWatchId;
        }

        public Rule Build() {
            return new Rule(Id, Transaction, Confirmations, WaitingTime, SuccessResponse,
                TimeoutResponse, Callback, CurrentWatchId);
        }
    }
}