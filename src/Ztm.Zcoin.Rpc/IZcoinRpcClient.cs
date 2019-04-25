using System;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin.RPC;

namespace Ztm.Zcoin.Rpc
{
    public interface IZcoinRpcClient : IDisposable
    {
        Task<BlockchainInfo> GetBlockchainInfoAsync(CancellationToken cancellationToken);
    }
}
