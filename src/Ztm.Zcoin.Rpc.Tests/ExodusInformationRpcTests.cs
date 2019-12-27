using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using Xunit;
using Ztm.Testing;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.Zcoin.Rpc.Tests
{
    public sealed class ExodusInformationRpcTests : RpcClientTesting
    {
        new ExodusInformationRpc Subject => (ExodusInformationRpc)base.Subject;

        [Fact]
        public Task GetBalanceAsync_WithNullAddress_ShouldThrow()
        {
            return Assert.ThrowsAsync<ArgumentNullException>(
                "address",
                () => Subject.GetBalanceAsync(
                    null,
                    new Property(new PropertyId(1), PropertyType.Divisible),
                    CancellationToken.None
                )
            );
        }

        [Fact]
        public Task GetBalanceAsync_WithNullProperty_ShouldThrow()
        {
            return Assert.ThrowsAsync<ArgumentNullException>(
                "property",
                () => Subject.GetBalanceAsync(TestAddress.Regtest1, null, CancellationToken.None)
            );
        }

        [Fact]
        public async Task GetBalanceAsync_WithValidAddressAndProperty_ShouldReturnExpectedData()
        {
            // Arrange.
            var owner = await GenerateNewAddressAsync();
            var issuer = new PropertyIssuer(Factory);

            Node.Generate(101);
            await FundAddressAsync(owner, Money.Coins(1));
            Node.Generate(1);

            await issuer.IssueManagedAsync(owner);
            Node.Generate(1);

            var property = await GetPropertyAsync(issuer.Name);

            await GrantTokensAsync(property, owner, owner, new PropertyAmount(100));
            Node.Generate(1);

            // Act.
            var (balance, reserved) = await Subject.GetBalanceAsync(owner, property, CancellationToken.None);

            // Assert.
            Assert.Equal(new PropertyAmount(100), balance);
            Assert.Equal(PropertyAmount.Zero, reserved);
        }

        [Fact]
        public Task GetGrantsAsync_WithNullProperty_ShouldThrow()
        {
            return Assert.ThrowsAsync<ArgumentNullException>(
                "property",
                () => Subject.GetGrantsAsync(null, CancellationToken.None)
            );
        }

        [Fact]
        public async Task GetGrantsAsync_WithValidProperty_ShouldReturnCorrectData()
        {
            // Arrange.
            var owner = await GenerateNewAddressAsync();
            var receiver = await GenerateNewAddressAsync();
            var issuer = new PropertyIssuer(Factory);

            Node.Generate(101);
            await FundAddressAsync(owner, Money.Coins(1));
            Node.Generate(1);

            var create = await issuer.IssueManagedAsync(owner);
            Node.Generate(1);

            var property = await GetPropertyAsync(issuer.Name);

            var grant = await GrantTokensAsync(property, owner, receiver, PropertyAmount.One);
            Node.Generate(1);

            // Act.
            var result = await Subject.GetGrantsAsync(property, CancellationToken.None);

            // Assert.
            Assert.Equal(create.GetHash(), result.CreationTransaction);
            Assert.Single(result.Histories);
            Assert.Equal(property.Id, result.Id);
            Assert.Equal(owner, result.Issuer);
            Assert.Equal(issuer.Name, result.Name);
            Assert.Equal(PropertyAmount.One, result.TotalTokens);

            Assert.Equal(PropertyAmount.One, result.Histories.Single().Amount);
            Assert.Equal(grant, result.Histories.Single().Transaction);
            Assert.Equal(PropertyGrantType.Grant, result.Histories.Single().Type);
        }

        [Fact]
        public Task GetPayloadAsync_WithNullTx_ShouldThrow()
        {
            return Assert.ThrowsAsync<ArgumentNullException>(
                "tx",
                () => Subject.GetPayloadAsync(null, CancellationToken.None)
            );
        }

        [Fact]
        public async Task GetPayloadAsync_WithExodusTransaction_ShouldReturnItPayload()
        {
            // Arrange.
            var owner = await GenerateNewAddressAsync();
            var issuer = new PropertyIssuer(Factory);

            Node.Generate(101);
            await FundAddressAsync(owner, Money.Coins(1));
            Node.Generate(1);

            issuer.Ecosystem = Ecosystem.Main;
            issuer.Type = PropertyType.Indivisible;
            issuer.Current = null;
            issuer.Category = "Company";
            issuer.SubCategory = "Private";
            issuer.Name = "Satang Corporation";
            issuer.Url = "https://satang.com";
            issuer.Description = "Provides cryptocurrency solutions.";

            var tx = await issuer.IssueManagedAsync(owner);
            Node.Generate(1);

            // Act.
            var result = await Subject.GetPayloadAsync(tx.GetHash(), CancellationToken.None);

            // Assert.
            Assert.Equal(
                "0000003601000100000000436F6D70616E79005072697661746500536174616E6720436F72706F726174696F6E0068747470733A2F2F736174616E672E636F6D0050726F76696465732063727970746F63757272656E637920736F6C7574696F6E732E00",
                BitConverter.ToString(result).Replace("-", "")
            );
        }

        [Fact]
        public Task GetTransactionAsync_WithNullHash_ShouldThrow()
        {
            return Assert.ThrowsAsync<ArgumentNullException>(
                "hash",
                () => Subject.GetTransactionAsync(null, CancellationToken.None)
            );
        }

        [Fact]
        public async Task GetTransactionAsync_WithExodusTransaction_ShouldReturnCorrectData()
        {
            // Arrange.
            var owner = await GenerateNewAddressAsync();
            var issuer = new PropertyIssuer(Factory);

            Node.Generate(101);
            await FundAddressAsync(owner, Money.Coins(1));
            Node.Generate(1);

            var tx = await issuer.IssueManagedAsync(owner);
            var block = Node.Generate(1).Single();

            // Act.
            var result = await Subject.GetTransactionAsync(tx.GetHash(), CancellationToken.None);

            // Assert.
            Assert.Equal(103, result.Block);
            Assert.Equal(block, result.BlockHash);
            Assert.NotEqual(default(DateTimeOffset), result.BlockTime);
            Assert.Equal(1, result.Confirmations);
            Assert.NotEqual(Money.Zero, result.Fee);
            Assert.Null(result.InvalidReason);
            Assert.True(result.IsMine);
            Assert.Null(result.ReferenceAddress);
            Assert.Equal(owner, result.SendingAddress);
            Assert.Equal(tx.GetHash(), result.TxId);
            Assert.Equal("Create Property - Manual", result.Type);
            Assert.Equal(54, result.TypeInt);
            Assert.True(result.Valid);
            Assert.Equal(0, result.Version);
        }

        [Fact]
        public async Task ListPropertiesAsync_WhenInvoke_ShouldReturnCorrectData()
        {
            // Act.
            var result = await Subject.ListPropertiesAsync(CancellationToken.None);

            // Assert.
            Assert.Equal(2, result.Count());

            Assert.Equal("N/A", result.First().Category);
            Assert.Equal("Exodus serve as the binding between Zcoin, smart properties and contracts created on the Exodus Layer.", result.First().Description);
            Assert.Equal(new PropertyId(1), result.First().Id);
            Assert.Equal("Exodus", result.First().Name);
            Assert.Equal("N/A", result.First().Subcategory);
            Assert.Equal(PropertyType.Divisible, result.First().Type);
            Assert.Equal("https://www.zcoin.io", result.First().Url);

            Assert.Equal("N/A", result.Last().Category);
            Assert.Equal("Test Exodus serve as the binding between Zcoin, smart properties and contracts created on the Exodus Layer.", result.Last().Description);
            Assert.Equal(new PropertyId(2), result.Last().Id);
            Assert.Equal("Test Exodus", result.Last().Name);
            Assert.Equal("N/A", result.Last().Subcategory);
            Assert.Equal(PropertyType.Divisible, result.Last().Type);
            Assert.Equal("https://www.zcoin.io", result.Last().Url);
        }

        protected override RpcClient CreateSubject()
        {
            return new ExodusInformationRpc(Factory, Node.CreateRPCClient());
        }
    }
}
