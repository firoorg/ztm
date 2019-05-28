using System;
using System.Threading;
using System.Threading.Tasks;
using Ztm.Zcoin.NBitcoin;

namespace Ztm.Zcoin.Synchronization
{
    public interface IBlocksRetrieverHandler
    {
        Task<int> GetBlockHintAsync(CancellationToken cancellationToken);

        Task<int> ProcessBlockAsync(ZcoinBlock block, int height, CancellationToken cancellationToken);

        Task StopAsync(Exception ex, CancellationToken cancellationToken);
    }
}
