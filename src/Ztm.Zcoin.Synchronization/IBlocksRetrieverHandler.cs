using System;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using Ztm.Zcoin.NBitcoin;

namespace Ztm.Zcoin.Synchronization
{
    public interface IBlocksRetrieverHandler
    {
        Task<uint256> GetBlockHintAsync(CancellationToken cancellationToken);

        Task<uint256> ProcessBlockAsync(uint256 hash, ZcoinBlock block, CancellationToken cancellationToken);

        Task StopAsync(Exception ex, CancellationToken cancellationToken);
    }
}
