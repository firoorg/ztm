using System;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin.RPC;

namespace Ztm.Zcoin.Rpc
{
    public sealed class ZcoinRpcClient : IZcoinRpcClient
    {
        readonly RPCClient client;

        public ZcoinRpcClient(RPCClient client)
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

        public Task<BlockchainInfo> GetBlockchainInfoAsync(CancellationToken cancellationToken)
        {
            return this.client.GetBlockchainInfoAsync();
        }
    }
}
