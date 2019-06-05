using System.Threading;
using System.Threading.Tasks;
using Ztm.Zcoin.NBitcoin;

namespace Ztm.Zcoin.Synchronization
{
    public interface IBlockListener
    {
        Task BlockAddedAsync(ZcoinBlock block, int height);

        Task BlockRemovedAsync(ZcoinBlock block, int height);
    }
}
