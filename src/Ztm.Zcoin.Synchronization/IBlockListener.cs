using System.Threading;
using System.Threading.Tasks;
using NBitcoin;

namespace Ztm.Zcoin.Synchronization
{
    public interface IBlockListener
    {
        Task BlockAddedAsync(Block block, int height, CancellationToken cancellationToken);

        Task BlockRemovingAsync(Block block, int height, CancellationToken cancellationToken);
    }
}
