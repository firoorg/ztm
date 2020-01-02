using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using NBitcoin.RPC;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.Zcoin.Rpc
{
    public sealed class RpcFactory : IRpcFactory
    {
        readonly Uri serverUri;
        readonly RPCCredentialString credential;

        public RpcFactory(
            Network network,
            Uri server,
            RPCCredentialString credential,
            ITransactionEncoder exodusEncoder)
        {
            if (network == null)
            {
                throw new ArgumentNullException(nameof(network));
            }

            if (server == null)
            {
                throw new ArgumentNullException(nameof(server));
            }

            if (credential == null)
            {
                throw new ArgumentNullException(nameof(credential));
            }

            if (exodusEncoder == null)
            {
                throw new ArgumentNullException(nameof(exodusEncoder));
            }

            Network = network;
            this.serverUri = server;
            this.credential = credential;
            ExodusEncoder = exodusEncoder;

            GenesisTransactions = new HashSet<uint256>(Network.GetGenesis().Transactions.Select(t => t.GetHash()));
        }

        public ITransactionEncoder ExodusEncoder { get; }

        public ISet<uint256> GenesisTransactions { get; }

        public Network Network { get; }

        public async Task<IChainInformationRpc> CreateChainInformationRpcAsync(CancellationToken cancellationToken)
        {
            return new ChainInformationRpc(this, await CreateClientAsync(cancellationToken));
        }

        public async Task<IExodusInformationRpc> CreateExodusInformationRpcAsync(CancellationToken cancellationToken)
        {
            return new ExodusInformationRpc(this, await CreateClientAsync(cancellationToken));
        }

        public async Task<IPropertyManagementRpc> CreatePropertyManagementRpcAsync(CancellationToken cancellationToken)
        {
            return new PropertyManagementRpc(this, await CreateClientAsync(cancellationToken));
        }

        public async Task<IRawTransactionRpc> CreateRawTransactionRpcAsync(CancellationToken cancellationToken)
        {
            return new RawTransactionRpc(this, await CreateClientAsync(cancellationToken));
        }

        public async Task<IWalletRpc> CreateWalletRpcAsync(CancellationToken cancellationToken)
        {
            return new WalletRpc(this, await CreateClientAsync(cancellationToken));
        }

        async Task<RPCClient> CreateClientAsync(CancellationToken cancellationToken)
        {
            var client = new RPCClient(this.credential, this.serverUri, Network);

            await client.ScanRPCCapabilitiesAsync();

            return client;
        }
    }
}
