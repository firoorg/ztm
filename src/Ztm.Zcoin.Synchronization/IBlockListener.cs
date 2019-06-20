using System.Threading.Tasks;
using Ztm.ServiceModel;
using Ztm.Zcoin.NBitcoin;

namespace Ztm.Zcoin.Synchronization
{
    public interface IBlockListener : IBackgroundService
    {
        Task BlockAddedAsync(ZcoinBlock block, int height);

        Task BlockRemovingAsync(ZcoinBlock block, int height);
    }
}
