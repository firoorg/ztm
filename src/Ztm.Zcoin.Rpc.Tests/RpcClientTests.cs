using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NBitcoin;
using NBitcoin.RPC;
using NBitcoin.Tests;
using Xunit;
using Ztm.Zcoin.NBitcoin;
using Ztm.Zcoin.NBitcoin.Exodus;
using Ztm.Zcoin.Testing;

namespace Ztm.Zcoin.Rpc.Tests
{
    public class RpcClientTests : IDisposable
    {
        static readonly IEnumerable<ITransactionPayloadEncoder> ExodusEncoders = new[]
        {
            new SimpleSendEncoder()
        };

        readonly NodeBuilder nodes;
        readonly Lazy<RpcClient> subject;
        bool disposed;

        public RpcClientTests()
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

                this.subject = new Lazy<RpcClient>(CreateSubject, LazyThreadSafetyMode.ExecutionAndPublication);
            }
            catch
            {
                this.nodes.Dispose();
                throw;
            }
        }

        protected RPCClient Client { get; }

        protected RpcFactory Factory { get; }

        protected Network Network => this.nodes.Network;

        protected CoreNode Node { get; }

        protected RpcClient Subject => this.subject.Value;

        FakeRpcClient FakeSubject => (FakeRpcClient)Subject;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        [Fact]
        public void Constructor_WithNullFactory_ShouldThrow()
        {
            var client = Node.CreateRPCClient();

            Assert.Throws<ArgumentNullException>("factory", () => new FakeRpcClient(null, client));
        }

        [Fact]
        public void Constructor_WithNullClient_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>("client", () => new FakeRpcClient(Factory, null));
        }

        [Fact]
        public void Constructor_WhenSuccess_ShouldInitializeProperties()
        {
            Assert.Same(Client, FakeSubject.Client);
            Assert.Same(Factory, FakeSubject.Factory);
        }

        [Fact]
        public void Dispose_WhenInvoke_ShouldInvokeProtectedVersion()
        {
            FakeSubject.Dispose();
            FakeSubject.Dispose();

            FakeSubject.StubbedDispose.Verify(
                f => f(true),
                Times.Exactly(2)
            );
        }

        [Fact]
        public void ToNative_WithInvalidEcosystem_ShouldThrow()
        {
            Assert.Throws<ArgumentOutOfRangeException>("ecosystem", () => FakeRpcClient.ToNative((Ecosystem)99));
        }

        [Theory]
        [InlineData(Ecosystem.Main, 1)]
        [InlineData(Ecosystem.Test, 2)]
        public void ToNative_WithValidEcosystem_ShouldReturnCorrectValue(Ecosystem ecosystem, byte expected)
        {
            var result = FakeRpcClient.ToNative(ecosystem);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void ToNative_WithNullPropertyId_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>("id", () => FakeRpcClient.ToNative(null));
        }

        [Fact]
        public void ToNative_WithValidPropertyId_ShouldReturnCorrectValue()
        {
            var id = new PropertyId(1);

            var result = FakeRpcClient.ToNative(id);

            Assert.Equal(1U, result);
        }

        [Fact]
        public void ToNative_WithInvalidPropertyType_ShouldThrow()
        {
            Assert.Throws<ArgumentOutOfRangeException>("type", () => FakeRpcClient.ToNative((PropertyType)99));
        }

        [Theory]
        [InlineData(PropertyType.Indivisible, 1)]
        [InlineData(PropertyType.Divisible, 2)]
        public void ToNative_WithValidPropertyType_ShouldReturnCorrectValue(PropertyType type, ushort expected)
        {
            var result = FakeRpcClient.ToNative(type);

            Assert.Equal(expected, result);
        }

        [Fact]
        public Task PopulateExodusInformationAsync_WithNullTx_ShouldThrow()
        {
            return Assert.ThrowsAsync<ArgumentNullException>(
                "tx",
                () => FakeSubject.PopulateExodusInformationAsync(null, CancellationToken.None)
            );
        }

        [Fact]
        public async Task PopulateExodusInformationAsync_WithGenesisTx_ShouldNotPopulate()
        {
            foreach (var tx in Network.GetGenesis().Transactions)
            {
                await FakeSubject.PopulateExodusInformationAsync(tx, CancellationToken.None);

                Assert.Null(tx.GetExodusTransaction());
            }
        }

        [Fact]
        public Task PopulateExodusInformationAsync_WithInvalidTx_ShouldThrow()
        {
            var tx = Transaction.Create(Network);

            return Assert.ThrowsAsync<RPCException>(
                () => FakeSubject.PopulateExodusInformationAsync(tx, CancellationToken.None)
            );
        }

        [Fact]
        public async Task PopulateExodusInformationAsync_WithMemPoolTx_ShouldThrow()
        {
            // Arrange.
            var owner = await GenerateNewAddressAsync();
            var receiver = await GenerateNewAddressAsync();
            var issuer = new PropertyIssuer(Factory);

            Node.Generate(101);
            await FundAddressAsync(owner, Money.Coins(1));
            Node.Generate(1);

            await issuer.IssueManagedAsync(owner);
            Node.Generate(1);

            var property = await GetPropertyAsync(issuer.Name);

            await GrantTokensAsync(property, owner, owner, new PropertyAmount(100));
            Node.Generate(1);

            var tx = await SendTokensAsync(property, owner, receiver, new PropertyAmount(10));

            // Act.
            await Assert.ThrowsAsync<ArgumentException>(
                "tx",
                () => FakeSubject.PopulateExodusInformationAsync(tx, CancellationToken.None)
            );
        }

        [Fact]
        public async Task PopulateExodusInformationAsync_WithNonExodusTx_ShouldNotPopulate()
        {
            // Arrange.
            var receiver = await GenerateNewAddressAsync();

            Node.Generate(101);
            await FundAddressAsync(receiver, Money.Coins(10));

            var hash = Node.Generate(1).Single();
            var block = await GetBlockAsync(hash);

            var coinbase = block.Transactions[0];
            var transfer = block.Transactions[1];

            // Act.
            await FakeSubject.PopulateExodusInformationAsync(coinbase, CancellationToken.None);
            await FakeSubject.PopulateExodusInformationAsync(transfer, CancellationToken.None);

            // Assert.
            Assert.Null(coinbase.GetExodusTransaction());
            Assert.Null(transfer.GetExodusTransaction());
        }

        [Fact]
        public async Task PopulateExodusInformationAsync_WithUnsupportedTx_ShouldNotPopulate()
        {
            // Arrange.
            var owner = await GenerateNewAddressAsync();
            var issuer = new PropertyIssuer(Factory);

            Node.Generate(101);
            await FundAddressAsync(owner, Money.Coins(1));
            Node.Generate(1);

            var tx = await issuer.IssueManagedAsync(owner);
            Node.Generate(1);

            // Act.
            await FakeSubject.PopulateExodusInformationAsync(tx, CancellationToken.None);

            // Assert.
            Assert.Null(tx.GetExodusTransaction());
        }

        [Fact]
        public async Task PopulateExodusInformationAsync_WithSupportedTx_ShouldPopulate()
        {
            // Arrange.
            var owner = await GenerateNewAddressAsync();
            var receiver = await GenerateNewAddressAsync();
            var issuer = new PropertyIssuer(Factory);

            Node.Generate(101);
            await FundAddressAsync(owner, Money.Coins(1));
            Node.Generate(1);

            await issuer.IssueManagedAsync(owner);
            Node.Generate(1);

            var property = await GetPropertyAsync(issuer.Name);

            await GrantTokensAsync(property, owner, owner, new PropertyAmount(100));
            Node.Generate(1);

            var tx = await SendTokensAsync(property, owner, receiver, PropertyAmount.One);
            Node.Generate(1);

            // Act.
            await FakeSubject.PopulateExodusInformationAsync(tx, CancellationToken.None);

            // Assert.
            var exodus = (SimpleSendV0)tx.GetExodusTransaction();

            Assert.Equal(PropertyAmount.One, exodus.Amount);
            Assert.Equal(property.Id, exodus.Property);
            Assert.Equal(receiver, exodus.Receiver);
            Assert.Equal(owner, exodus.Sender);
        }

        protected virtual RpcClient CreateSubject()
        {
            return new FakeRpcClient(Factory, Client);
        }

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
