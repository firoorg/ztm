using System;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using NBitcoin.RPC;
using Ztm.Zcoin.NBitcoin;

namespace Ztm.Zcoin.Rpc
{
    public interface IZcoinRpcClient : IDisposable
    {
        Task<ZcoinBlock> GetBlockAsync(uint256 blockHash, CancellationToken cancellationToken);

        Task<ZcoinBlock> GetBlockAsync(int height, CancellationToken cancellationToken);

        Task<ZcoinBlockHeader> GetBlockHeaderAsync(uint256 blockHash, CancellationToken cancellationToken);

        Task<ZcoinBlockHeader> GetBlockHeaderAsync(int height, CancellationToken cancellationToken);

        Task<BlockchainInfo> GetBlockchainInfoAsync(CancellationToken cancellationToken);
    }
}
