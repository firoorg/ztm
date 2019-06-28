using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ztm.Zcoin.NBitcoin;
using Ztm.Zcoin.Synchronization.Watchers;
using Ztm.Zcoin.Synchronization.Watchers.Rules;

namespace Ztm.Zcoin.Synchronization.Tests.Watchers.Rules
{
    class TestExpirableRulesExecutor : ExpirableRulesExecutor<ExpirableRule, RuledWatch<ExpirableRule>>
    {
        public TestExpirableRulesExecutor(
            IExpirableRulesStorage<ExpirableRule> storage,
            IRulesExpireWatcher<ExpirableRule, RuledWatch<ExpirableRule>> expireWatcher) : base(storage, expireWatcher)
        {
        }

        public Func<RuledWatch<ExpirableRule>, WatchRemoveReason, bool> DisassociateRule { get; set; }

        public Func<ZcoinBlock, int, IEnumerable<RuledWatch<ExpirableRule>>> ExecuteRules { get; set; }

        public Action<ExpirableRule> OnRuleExpired { get; set; }

        public Task AddRuleAsync(ExpirableRule rule, CancellationToken cancellationToken)
        {
            return ((IRulesExecutor<ExpirableRule, RuledWatch<ExpirableRule>>)this).AddRuleAsync(
                rule,
                cancellationToken
            );
        }

        public Task DisassociateRulesAsyc(
            IEnumerable<WatchToRemove<RuledWatch<ExpirableRule>>> watches,
            CancellationToken cancellationToken)
        {
            return ((IRulesExecutor<ExpirableRule, RuledWatch<ExpirableRule>>)this).DisassociateRulesAsyc(
                watches,
                cancellationToken
            );
        }

        protected override Task<bool> DisassociateRuleAsyc(
            RuledWatch<ExpirableRule> watch,
            WatchRemoveReason reason,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(DisassociateRule(watch, reason));
        }

        protected override Task<IEnumerable<RuledWatch<ExpirableRule>>> ExecuteRulesAsync(
            ZcoinBlock block,
            int height,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(ExecuteRules(block, height));
        }

        protected override Task OnRuleExpiredAsync(ExpirableRule rule, CancellationToken cancellationToken)
        {
            OnRuleExpired(rule);
            return Task.CompletedTask;
        }
    }
}
