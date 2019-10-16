using System.Threading;
using System.Threading.Tasks;
using Ztm.Zcoin.NBitcoin;

namespace Ztm.Zcoin.Synchronization
{
    public interface IBlockListener
    {
        Task BlockAddedAsync(ZcoinBlock block, int height, CancellationToken cancellationToken);

        Task BlockRemovingAsync(ZcoinBlock block, int height, CancellationToken cancellationToken);
    }
}
