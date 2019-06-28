using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ztm.Zcoin.Synchronization.Watchers.Rules
{
    public interface IRulesStorage<T> where T : Rule
    {
        Task AddRuleAsync(T rule, CancellationToken cancellationToken);

        Task RemoveRulesAsync(IEnumerable<T> rules, CancellationToken cancellationToken);
    }
}
