using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using Ztm.Zcoin.NBitcoin.Exodus;
using Watch = Ztm.Zcoin.Watching.BalanceWatch<Ztm.WebApi.Watchers.TokenReceiving.Rule, Ztm.Zcoin.NBitcoin.Exodus.PropertyAmount>;

namespace Ztm.WebApi.Watchers.TokenReceiving
{
    public interface IWatchRepository
    {
        Task AddAsync(IEnumerable<Watch> watches, CancellationToken cancellationToken);

        Task<IEnumerable<Watch>> ListUncompletedAsync(PropertyId property, CancellationToken cancellationToken);

        Task SetConfirmationCountAsync(IReadOnlyDictionary<Watch, int> watches, CancellationToken cancellationToken);

        Task<IReadOnlyDictionary<Watch, int>> TransitionToRejectedAsync(
            PropertyId property,
            uint256 startBlock,
            CancellationToken cancellationToken);

        Task<IReadOnlyDictionary<Watch, int>> TransitionToSucceededAsync(
            IEnumerable<Watch> watches,
            CancellationToken cancellationToken);

        Task<IReadOnlyDictionary<Watch, int>> TransitionToTimedOutAsync(Rule rule, CancellationToken cancellationToken);
    }
}
