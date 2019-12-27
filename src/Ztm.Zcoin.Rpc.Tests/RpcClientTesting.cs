using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using NBitcoin.RPC;
using NBitcoin.Tests;
using Ztm.Zcoin.NBitcoin.Exodus;
using Ztm.Zcoin.Testing;

namespace Ztm.Zcoin.Rpc.Tests
{
    public abstract class RpcClientTesting : IDisposable
    {
        static readonly IEnumerable<ITransactionPayloadEncoder> ExodusEncoders = new[]
        {
            new SimpleSendEncoder()
        };

        readonly NodeBuilder nodes;
        readonly Lazy<RpcClient> subject;
        bool disposed;

        protected RpcClientTesting()
        {
            this.nodes = NodeBuilderFactory.CreateNodeBuilder(GetType());

            try
            {
                this.nodes.ConfigParameters.Add("autocommit", "0");
                this.nodes.ConfigParameters.Add("dandelion", "0");
                this.nodes.ConfigParameters.Add("exodus", "1");

                Node = this.nodes.CreateNode(true);
                Client = Node.CreateRPCClient();

                Factory = new RpcFactory(
                    this.nodes.Network,
                    Node.RPCUri,
                    RPCCredentialString.Parse(Node.GetRPCAuth()),
                    new TransactionEncoder(ExodusEncoders)
                );

                this.subject = new Lazy<RpcClient>(CreateSubject, LazyThreadSafetyMode.ExecutionAndPublication); // lgtm[cs/virtual-call-in-constructor]
            }
            catch
            {
                this.nodes.Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected RPCClient Client { get; }

        protected RpcFactory Factory { get; }

        protected Network Network => this.nodes.Network;

        protected CoreNode Node { get; }

        protected RpcClient Subject => this.subject.Value;

        protected abstract RpcClient CreateSubject();

        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            if (disposing)
            {
                if (this.subject.IsValueCreated)
                {
                    this.subject.Value.Dispose();
                }

                this.nodes.Dispose();
            }

            this.disposed = true;
        }

        protected async Task<uint256> FundAddressAsync(BitcoinAddress address, Money amount)
        {
            using (var rpc = await Factory.CreateWalletRpcAsync(CancellationToken.None))
            {
                return await rpc.SendAsync(address, amount, null, null, false, CancellationToken.None);
            }
        }

        protected async Task<BitcoinAddress> GenerateNewAddressAsync()
        {
            using (var rpc = await Factory.CreateWalletRpcAsync(CancellationToken.None))
            {
                return await rpc.GetNewAddressAsync(CancellationToken.None);
            }
        }

        protected async Task<Block> GetBlockAsync(uint256 hash)
        {
            using (var rpc = await Factory.CreateChainInformationRpcAsync(CancellationToken.None))
            {
                return await rpc.GetBlockAsync(hash, CancellationToken.None);
            }
        }

        protected async Task<Property> GetPropertyAsync(string name)
        {
            using (var rpc = await Factory.CreateExodusInformationRpcAsync(CancellationToken.None))
            {
                var props = await rpc.ListPropertiesAsync(CancellationToken.None);
                var prop = props.Single(p => p.Name == name);

                return new Property(prop.Id, prop.Type);
            }
        }

        protected async Task<uint256> GrantTokensAsync(
            Property property,
            BitcoinAddress from,
            BitcoinAddress to,
            PropertyAmount amount)
        {
            Transaction tx;

            using (var rpc = await Factory.CreatePropertyManagementRpcAsync(CancellationToken.None))
            {
                tx = await rpc.GrantAsync(property, from, to, amount, null, CancellationToken.None);
            }

            return await SendTransactionAsync(tx);
        }

        protected async Task<Transaction> SendTokensAsync(
            Property property,
            BitcoinAddress from,
            BitcoinAddress to,
            PropertyAmount amount)
        {
            Transaction tx;

            using (var rpc = await Factory.CreatePropertyManagementRpcAsync(CancellationToken.None))
            {
                tx = await rpc.SendAsync(from, to, property, amount, null, CancellationToken.None);
            }

            using (var rpc = await Factory.CreateRawTransactionRpcAsync(CancellationToken.None))
            {
                await rpc.SendAsync(tx, CancellationToken.None);
            }

            return tx;
        }

        protected async Task<uint256> SendTransactionAsync(Transaction tx)
        {
            using (var rpc = await Factory.CreateRawTransactionRpcAsync(CancellationToken.None))
            {
                return await rpc.SendAsync(tx, CancellationToken.None);
            }
        }
    }
}
