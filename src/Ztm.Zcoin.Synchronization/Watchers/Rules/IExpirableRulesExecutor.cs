using System;

namespace Ztm.Zcoin.Synchronization.Watchers.Rules
{
    public interface IExpirableRulesExecutor<TRule, TWatch> : IRulesExecutor<TRule, TWatch>
        where TRule : ExpirableRule
        where TWatch : RuledWatch<TRule>
    {
        event EventHandler<RuleEventArgs<TRule>> RuleExpired;
    }
}
