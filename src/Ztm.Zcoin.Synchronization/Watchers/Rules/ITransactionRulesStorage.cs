using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;

namespace Ztm.Zcoin.Synchronization.Watchers.Rules
{
    public interface ITransactionRulesStorage : IExpirableRulesStorage<TransactionRule>
    {
        Task<IEnumerable<TransactionRule>> GetRulesByTransactionHashesAsync(
            IEnumerable<uint256> hashes,
            CancellationToken cancellationToken);
    }
}
