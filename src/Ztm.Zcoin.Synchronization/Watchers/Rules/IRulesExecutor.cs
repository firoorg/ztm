using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ztm.ServiceModel;
using Ztm.Zcoin.NBitcoin;

namespace Ztm.Zcoin.Synchronization.Watchers.Rules
{
    public interface IRulesExecutor<TRule, TWatch> : IBackgroundService
        where TRule : Rule
        where TWatch : RuledWatch<TRule>
    {
        Task AddRuleAsync(TRule rule, CancellationToken cancellationToken);

        Task DisassociateRulesAsyc(IEnumerable<WatchToRemove<TWatch>> watches, CancellationToken cancellationToken);

        Task<IEnumerable<TWatch>> ExecuteRulesAsync(ZcoinBlock block, int height, CancellationToken cancellationToken);
    }
}
