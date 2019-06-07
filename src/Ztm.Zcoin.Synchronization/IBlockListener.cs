using System.Threading.Tasks;
using Ztm.Zcoin.NBitcoin;

namespace Ztm.Zcoin.Synchronization
{
    public interface IBlockListener
    {
        Task BlockAddedAsync(ZcoinBlock block, int height);

        Task BlockRemovingAsync(ZcoinBlock block, int height);
    }
}
