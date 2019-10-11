using System.Threading;
using System.Threading.Tasks;
using Ztm.ServiceModel;
using Ztm.Zcoin.NBitcoin;

namespace Ztm.Zcoin.Synchronization
{
    public interface IBlockListener : IBackgroundService
    {
        Task BlockAddedAsync(ZcoinBlock block, int height, CancellationToken cancellationToken);

        Task BlockRemovingAsync(ZcoinBlock block, int height, CancellationToken cancellationToken);
    }
}
