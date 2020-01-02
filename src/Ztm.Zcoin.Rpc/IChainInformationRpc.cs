using System;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using NBitcoin.RPC;

namespace Ztm.Zcoin.Rpc
{
    public interface IChainInformationRpc : IDisposable
    {
        Task<Block> GetBlockAsync(uint256 hash, CancellationToken cancellationToken);
        Task<Block> GetBlockAsync(int height, CancellationToken cancellationToken);
        Task<BlockHeader> GetBlockHeaderAsync(uint256 hash, CancellationToken cancellationToken);
        Task<BlockHeader> GetBlockHeaderAsync(int height, CancellationToken cancellationToken);
        Task<BlockchainInfo> GetChainInfoAsync(CancellationToken cancellationToken);
    }
}
