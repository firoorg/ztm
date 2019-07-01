using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;

namespace Ztm.Zcoin.Synchronization.Watchers.Rules
{
    public interface IAddressRulesStorage : IExpirableRulesStorage<AddressRule>
    {
        Task<IEnumerable<AddressRule>> GetRulesByAddressesAsync(
            IEnumerable<BitcoinAddress> addresses,
            CancellationToken cancellationToken);
    }
}
