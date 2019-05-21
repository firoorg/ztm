using System;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using NBitcoin.RPC;
using Ztm.Zcoin.NBitcoin;

namespace Ztm.Zcoin.Rpc
{
    public sealed class ZcoinRpcClientFactory : IZcoinRpcClientFactory
    {
        readonly Uri serverUri;
        readonly NetworkType networkType;
        readonly RPCCredentialString credential;

        public ZcoinRpcClientFactory(Uri server, NetworkType type, RPCCredentialString credential)
        {
            if (server == null)
            {
                throw new ArgumentNullException(nameof(server));
            }

            if (credential == null)
            {
                throw new ArgumentNullException(nameof(credential));
            }

            this.serverUri = server;
            this.networkType = type;
            this.credential = credential;
        }

        public async Task<IZcoinRpcClient> CreateRpcClientAsync(CancellationToken cancellationToken)
        {
            var network = ZcoinNetworks.Instance.GetNetwork(this.networkType);
            var client = new Ztm.Zcoin.NBitcoin.RPC.ZcoinRPCClient(this.credential, this.serverUri, network);

            await client.ScanRPCCapabilitiesAsync();

            return new ZcoinRpcClient(client);
        }
    }
}
