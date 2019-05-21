using System;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using NBitcoin.RPC;
using Ztm.Zcoin.NBitcoin;

namespace Ztm.Zcoin.Rpc
{
    public sealed class ZcoinRpcClient : IZcoinRpcClient
    {
        readonly Ztm.Zcoin.NBitcoin.RPC.ZcoinRPCClient client;

        public ZcoinRpcClient(Ztm.Zcoin.NBitcoin.RPC.ZcoinRPCClient client)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            this.client = client;
        }

        public void Dispose()
        {
        }

        public Task<ZcoinBlock> GetBlockAsync(uint256 blockHash, CancellationToken cancellationToken)
        {
            return this.client.GetBlockAsync(blockHash);
        }

        public Task<ZcoinBlock> GetBlockAsync(int height, CancellationToken cancellationToken)
        {
            return this.client.GetBlockAsync(height);
        }

        public Task<ZcoinBlockHeader> GetBlockHeaderAsync(uint256 blockHash, CancellationToken cancellationToken)
        {
            return this.client.GetBlockHeaderAsync(blockHash);
        }

        public Task<ZcoinBlockHeader> GetBlockHeaderAsync(int height, CancellationToken cancellationToken)
        {
            return this.client.GetBlockHeaderAsync(height);
        }

        public Task<BlockchainInfo> GetBlockchainInfoAsync(CancellationToken cancellationToken)
        {
            return this.client.GetBlockchainInfoAsync();
        }
    }
}
