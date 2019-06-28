using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ztm.Zcoin.Synchronization.Watchers.Rules
{
    public interface IExpirableRulesStorage<T> : IRulesStorage<T> where T : ExpirableRule
    {
        Task<IEnumerable<T>> GetRulesAsync(CancellationToken cancellationToken);
    }
}
