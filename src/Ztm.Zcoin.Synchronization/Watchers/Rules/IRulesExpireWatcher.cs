using System;
using System.Threading;
using System.Threading.Tasks;
using Ztm.ServiceModel;

namespace Ztm.Zcoin.Synchronization.Watchers.Rules
{
    public interface IRulesExpireWatcher<TRule, TWatch> : IBackgroundService
        where TRule : ExpirableRule
        where TWatch : RuledWatch<TRule>
    {
        event EventHandler<RuleEventArgs<TRule>> RuleExpired;

        Task<bool> AddReferenceAsync(TWatch watch, CancellationToken cancellationToken);

        Task AddRuleAsync(TRule rule, CancellationToken cancellationToken);

        bool IsSupported(Type policy);

        Task RemoveReferenceAsync(TWatch watch, bool ruleAlive, CancellationToken cancellationToken);
    }
}
