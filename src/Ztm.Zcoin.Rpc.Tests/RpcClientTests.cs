using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NBitcoin;
using NBitcoin.RPC;
using Xunit;
using Ztm.Zcoin.NBitcoin;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.Zcoin.Rpc.Tests
{
    public sealed class RpcClientTests : RpcClientTesting
    {
        new FakeRpcClient Subject => (FakeRpcClient)base.Subject;

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
            Assert.Same(Client, Subject.Client);
            Assert.Same(Factory, Subject.Factory);
        }

        [Fact]
        public void Dispose_WhenInvoke_ShouldInvokeProtectedVersion()
        {
            Subject.Dispose();
            Subject.Dispose();

            Subject.StubbedDispose.Verify(
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
                () => Subject.PopulateExodusInformationAsync(null, CancellationToken.None)
            );
        }

        [Fact]
        public async Task PopulateExodusInformationAsync_WithGenesisTx_ShouldNotPopulate()
        {
            foreach (var tx in Network.GetGenesis().Transactions)
            {
                await Subject.PopulateExodusInformationAsync(tx, CancellationToken.None);

                Assert.Null(tx.GetExodusTransaction());
            }
        }

        [Fact]
        public Task PopulateExodusInformationAsync_WithInvalidTx_ShouldThrow()
        {
            var tx = Transaction.Create(Network);

            return Assert.ThrowsAsync<RPCException>(
                () => Subject.PopulateExodusInformationAsync(tx, CancellationToken.None)
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
                () => Subject.PopulateExodusInformationAsync(tx, CancellationToken.None)
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
            await Subject.PopulateExodusInformationAsync(coinbase, CancellationToken.None);
            await Subject.PopulateExodusInformationAsync(transfer, CancellationToken.None);

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
            await Subject.PopulateExodusInformationAsync(tx, CancellationToken.None);

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
            await Subject.PopulateExodusInformationAsync(tx, CancellationToken.None);

            // Assert.
            var exodus = (SimpleSendV0)tx.GetExodusTransaction();

            Assert.Equal(PropertyAmount.One, exodus.Amount);
            Assert.Equal(property.Id, exodus.Property);
            Assert.Equal(receiver, exodus.Receiver);
            Assert.Equal(owner, exodus.Sender);
        }

        protected override RpcClient CreateSubject()
        {
            return new FakeRpcClient(Factory, Client);
        }
    }
}
