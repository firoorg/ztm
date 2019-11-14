using System;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using NBitcoin.RPC;
using Ztm.Zcoin.NBitcoin;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.Zcoin.Rpc
{
    public sealed class ZcoinRpcClientFactory : IZcoinRpcClientFactory
    {
        readonly Uri serverUri;
        readonly NetworkType networkType;
        readonly RPCCredentialString credential;
        readonly ITransactionEncoder encoder;

        public ZcoinRpcClientFactory(Uri server, NetworkType type, RPCCredentialString credential, ITransactionEncoder encoder)
        {
            if (server == null)
            {
                throw new ArgumentNullException(nameof(server));
            }

            if (credential == null)
            {
                throw new ArgumentNullException(nameof(credential));
            }

            if (encoder == null)
            {
                throw new ArgumentNullException(nameof(encoder));
            }

            this.serverUri = server;
            this.networkType = type;
            this.credential = credential;
            this.encoder = encoder;
        }

        public async Task<IZcoinRpcClient> CreateRpcClientAsync(CancellationToken cancellationToken)
        {
            var network = ZcoinNetworks.Instance.GetNetwork(this.networkType);
            var client = new RPCClient(this.credential, this.serverUri, network);

            await client.ScanRPCCapabilitiesAsync();

            return new ZcoinRpcClient(client, encoder);
        }
    }
}
