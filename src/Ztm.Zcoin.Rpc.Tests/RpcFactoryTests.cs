using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NBitcoin;
using NBitcoin.RPC;
using NBitcoin.Tests;
using Xunit;
using Ztm.Zcoin.NBitcoin.Exodus;
using Ztm.Zcoin.Testing;

namespace Ztm.Zcoin.Rpc.Tests
{
    public sealed class RpcFactoryTests : IDisposable
    {
        readonly NodeBuilder nodes;
        readonly Network network;
        readonly Uri server;
        readonly RPCCredentialString credential;
        readonly Mock<ITransactionEncoder> exodusEncoder;
        readonly RpcFactory subject;

        public RpcFactoryTests()
        {
            this.nodes = NodeBuilderFactory.CreateNodeBuilder(GetType());

            try
            {
                var node = this.nodes.CreateNode(true);

                this.network = node.Network;
                this.server = node.RPCUri;
                this.credential = RPCCredentialString.Parse(node.GetRPCAuth());
                this.exodusEncoder = new Mock<ITransactionEncoder>();
                this.subject = new RpcFactory(this.network, this.server, this.credential, this.exodusEncoder.Object);
            }
            catch
            {
                this.nodes.Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            this.nodes.Dispose();
        }

        [Fact]
        public void Constructor_WithNullNetwork_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "network",
                () => new RpcFactory(null, this.server, this.credential, this.exodusEncoder.Object)
            );
        }

        [Fact]
        public void Constructor_WithNullServer_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "server",
                () => new RpcFactory(this.network, null, this.credential, this.exodusEncoder.Object)
            );
        }

        [Fact]
        public void Constructor_WithNullCredential_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "credential",
                () => new RpcFactory(this.network, this.server, null, this.exodusEncoder.Object)
            );
        }

        [Fact]
        public void Constructor_WithNullExodusEncoder_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "exodusEncoder",
                () => new RpcFactory(this.network, this.server, this.credential, null)
            );
        }

        [Fact]
        public void Constructor_WhenSuccess_ShouldInitializeProperties()
        {
            this.subject.ExodusEncoder.Should().BeSameAs(this.exodusEncoder.Object);
            this.subject.GenesisTransactions.Should().BeEquivalentTo(this.network.GetGenesis().Transactions.Select(t => t.GetHash()));
            this.subject.Network.Should().BeSameAs(this.network);
        }

        [Fact]
        public async Task CreateChainInformationRpcAsync_WhenInvoke_ShouldReturnNonNull()
        {
            var result = await this.subject.CreateChainInformationRpcAsync(CancellationToken.None);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task CreateExodusInformationRpcAsync_WhenInvoke_ShouldReturnNonNull()
        {
            var result = await this.subject.CreateExodusInformationRpcAsync(CancellationToken.None);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task CreatePropertyManagementRpcAsync_WhenInvoke_ShouldReturnNonNull()
        {
            var result = await this.subject.CreatePropertyManagementRpcAsync(CancellationToken.None);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task CreateRawTransactionRpcAsync_WhenInvoke_ShouldReturnNonNull()
        {
            var result = await this.subject.CreateRawTransactionRpcAsync(CancellationToken.None);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task CreateWalletRpcAsync_WhenInvoke_ShouldReturnNonNull()
        {
            var result = await this.subject.CreateWalletRpcAsync(CancellationToken.None);

            Assert.NotNull(result);
        }
    }
}
