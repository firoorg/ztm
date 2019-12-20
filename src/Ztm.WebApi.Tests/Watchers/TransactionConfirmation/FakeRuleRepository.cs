using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using Ztm.WebApi.Callbacks;
using Ztm.WebApi.Watchers.TransactionConfirmation;

namespace Ztm.WebApi.Tests.Watchers.TransactionConfirmation
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

            var rule = new Rule(id, transaction, confirmation, unconfirmedWaitingTime, successResponse, timeoutResponse, callback, DateTime.UtcNow);

            this.rules.Add(id, new RuleWithAdditionalDatas
            {
                Rule = rule,
                RemainingTime = unconfirmedWaitingTime,
                Status = RuleStatus.Pending,
                CurrentWatchId = null,
            });

            return Task.FromResult(rule);
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

        public virtual Task<IEnumerable<Rule>> ListWaitingAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(this.rules.Where(r => r.Value.Status == RuleStatus.Pending).Select(r => r.Value.Rule).AsEnumerable());
        }

        public virtual Task SubtractRemainingWaitingTimeAsync(Guid id, TimeSpan consumedTime, CancellationToken cancellationToken)
        {
            this.update(id,
                (old) =>
                {
                    old.RemainingTime -= consumedTime;
                    old.RemainingTime = old.RemainingTime < TimeSpan.Zero ? TimeSpan.Zero : old.RemainingTime;
                    return old;
                }
            );

            return Task.CompletedTask;
        }

        public virtual Task UpdateCurrentWatchAsync(Guid id, Guid? watchId, CancellationToken cancellationToken)
        {
            this.update(id,
                (old) =>
                {
                    old.CurrentWatchId = watchId;
                    return old;
                }
            );

            return Task.CompletedTask;
        }

        public virtual Task UpdateStatusAsync(Guid id, RuleStatus status, CancellationToken cancellationToken)
        {
            this.update(id,
                (old) =>
                {
                    old.Status = status;
                    return old;
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
        public Guid? CurrentWatchId { get; set; }
    }

    class RuleBuilder
    {
        public Guid Id { get; set; }
        public uint256 TransactionHash { get; set; }
        public int Confirmations { get; set; }
        public TimeSpan WaitingTime { get; set; }
        public CallbackResult SuccessResponse { get; set; }
        public CallbackResult TimeoutResponse { get; set; }
        public Callback Callback { get; set; }

        public RuleBuilder(Rule old)
        {
            Id = old.Id;
            TransactionHash = old.TransactionHash;
            Confirmations = old.Confirmations;
            WaitingTime = old.OriginalWaitingTime;
            SuccessResponse = old.SuccessResponse;
            TimeoutResponse = old.TimeoutResponse;
            Callback = old.Callback;
        }

        public Rule Build() {
            return new Rule(Id, TransactionHash, Confirmations, WaitingTime, SuccessResponse,
                TimeoutResponse, Callback, DateTime.UtcNow);
        }
    }
}