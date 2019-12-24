using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;

namespace Ztm.Zcoin.Watching
{
    public interface IWatcherHandler<TContext, TWatch> where TWatch : Watch<TContext>
    {
        Task AddWatchesAsync(IEnumerable<TWatch> watches, CancellationToken cancellationToken);

        Task RemoveBlockRemovingWatchesAsync(uint256 block, CancellationToken cancellationToken);

        Task RemoveCompletedWatchesAsync(IEnumerable<TWatch> watches, CancellationToken cancellationToken);
    }
}
