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
    public sealed class PropertyManagementRpcTests : RpcClientTesting
    {
        new PropertyManagementRpc Subject => (PropertyManagementRpc)base.Subject;

        [Fact]
        public Task CreateManagedAsync_WithNullOwner_ShouldThrow()
        {
            return Assert.ThrowsAsync<ArgumentNullException>(
                "owner",
                () => Subject.CreateManagedAsync(
                    null,
                    Ecosystem.Main,
                    PropertyType.Divisible,
                    null,
                    "Company",
                    "Private",
                    "Satang Corporation",
                    "https://satang.com",
                    "Provides cryptocurrency solutions.",
                    CancellationToken.None
                )
            );
        }

        [Fact]
        public Task CreateManagedAsync_WithNullCategory_ShouldThrow()
        {
            return Assert.ThrowsAsync<ArgumentNullException>(
                "category",
                () => Subject.CreateManagedAsync(
                    TestAddress.Regtest1,
                    Ecosystem.Main,
                    PropertyType.Divisible,
                    null,
                    null,
                    "Private",
                    "Satang Corporation",
                    "https://satang.com",
                    "Provides cryptocurrency solutions.",
                    CancellationToken.None
                )
            );
        }

        [Fact]
        public Task CreateManagedAsync_WithNullSubcategory_ShouldThrow()
        {
            return Assert.ThrowsAsync<ArgumentNullException>(
                "subcategory",
                () => Subject.CreateManagedAsync(
                    TestAddress.Regtest1,
                    Ecosystem.Main,
                    PropertyType.Divisible,
                    null,
                    "Company",
                    null,
                    "Satang Corporation",
                    "https://satang.com",
                    "Provides cryptocurrency solutions.",
                    CancellationToken.None
                )
            );
        }

        [Fact]
        public Task CreateManagedAsync_WithNullName_ShouldThrow()
        {
            return Assert.ThrowsAsync<ArgumentNullException>(
                "name",
                () => Subject.CreateManagedAsync(
                    TestAddress.Regtest1,
                    Ecosystem.Main,
                    PropertyType.Divisible,
                    null,
                    "Company",
                    "Private",
                    null,
                    "https://satang.com",
                    "Provides cryptocurrency solutions.",
                    CancellationToken.None
                )
            );
        }

        [Fact]
        public Task CreateManagedAsync_WithNullUrl_ShouldThrow()
        {
            return Assert.ThrowsAsync<ArgumentNullException>(
                "url",
                () => Subject.CreateManagedAsync(
                    TestAddress.Regtest1,
                    Ecosystem.Main,
                    PropertyType.Divisible,
                    null,
                    "Company",
                    "Private",
                    "Satang Corporation",
                    null,
                    "Provides cryptocurrency solutions.",
                    CancellationToken.None
                )
            );
        }

        [Fact]
        public Task CreateManagedAsync_WithNullDescription_ShouldThrow()
        {
            return Assert.ThrowsAsync<ArgumentNullException>(
                "description",
                () => Subject.CreateManagedAsync(
                    TestAddress.Regtest1,
                    Ecosystem.Main,
                    PropertyType.Divisible,
                    null,
                    "Company",
                    "Private",
                    "Satang Corporation",
                    "https://satang.com",
                    null,
                    CancellationToken.None
                )
            );
        }

        [Fact]
        public async Task CreateManagedAsync_WithValidArgs_ShouldCreateNewManagedProperty()
        {
            // Arrange.
            var owner = await GenerateNewAddressAsync();

            Node.Generate(101);
            await FundAddressAsync(owner, Money.Coins(1));
            Node.Generate(1);

            // Act.
            var tx = await Subject.CreateManagedAsync(
                owner,
                Ecosystem.Main,
                PropertyType.Indivisible,
                null,
                "Company",
                "Private",
                "Satang Corporation",
                "https://satang.com",
                "Provides cryptocurrency solutions.",
                CancellationToken.None
            );

            await SendTransactionAsync(tx);
            Node.Generate(1);

            // Assert.
            using (var rpc = await Factory.CreateExodusInformationRpcAsync(CancellationToken.None))
            {
                var props = await rpc.ListPropertiesAsync(CancellationToken.None);

                Assert.Equal(3, props.Count());
                Assert.Equal(3, props.Last().Id.Value);
                Assert.Equal("Satang Corporation", props.Last().Name);
                Assert.Equal("Company", props.Last().Category);
                Assert.Equal("Private", props.Last().Subcategory);
                Assert.Equal("https://satang.com", props.Last().Url);
                Assert.Equal("Provides cryptocurrency solutions.", props.Last().Description);
                Assert.Equal(PropertyType.Indivisible, props.Last().Type);
            }
        }

        [Fact]
        public Task GrantAsync_WithNullProperty_ShouldThrow()
        {
            return Assert.ThrowsAsync<ArgumentNullException>(
                "property",
                () => Subject.GrantAsync(
                    null,
                    TestAddress.Regtest1,
                    TestAddress.Regtest2,
                    new PropertyAmount(100),
                    null,
                    CancellationToken.None
                )
            );
        }

        [Fact]
        public Task GrantAsync_WithNullFrom_ShouldThrow()
        {
            return Assert.ThrowsAsync<ArgumentNullException>(
                "from",
                () => Subject.GrantAsync(
                    new Property(new PropertyId(3), PropertyType.Indivisible),
                    null,
                    TestAddress.Regtest1,
                    new PropertyAmount(100),
                    null,
                    CancellationToken.None
                )
            );
        }

        [Fact]
        public Task GrantAsync_WithNullTo_ShouldThrow()
        {
            return Assert.ThrowsAsync<ArgumentNullException>(
                "to",
                () => Subject.GrantAsync(
                    new Property(new PropertyId(3), PropertyType.Indivisible),
                    TestAddress.Regtest1,
                    null,
                    new PropertyAmount(100),
                    null,
                    CancellationToken.None
                )
            );
        }

        [Fact]
        public Task GrantAsync_WithInvalidAmount_ShouldThrow()
        {
            return Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                "amount",
                () => Subject.GrantAsync(
                    new Property(new PropertyId(3), PropertyType.Indivisible),
                    TestAddress.Regtest1,
                    TestAddress.Regtest2,
                    PropertyAmount.Zero,
                    null,
                    CancellationToken.None
                )
            );
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("abc")]
        public async Task GrantAsync_WithValidArgs_ShouldGrantSuccess(string note)
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

            // Act.
            var tx = await Subject.GrantAsync(
                property,
                owner,
                receiver,
                new PropertyAmount(100),
                note,
                CancellationToken.None
            );

            await SendTransactionAsync(tx);
            Node.Generate(1);

            // Assert.
            using (var rpc = await Factory.CreateExodusInformationRpcAsync(CancellationToken.None))
            {
                var grant = await rpc.GetGrantsAsync(property, CancellationToken.None);

                Assert.Equal(create.GetHash(), grant.CreationTransaction);
                Assert.Single(grant.Histories);
                Assert.Equal(property.Id, grant.Id);
                Assert.Equal(owner, grant.Issuer);
                Assert.Equal(issuer.Name, grant.Name);
                Assert.Equal(new PropertyAmount(100), grant.TotalTokens);

                Assert.Equal(new PropertyAmount(100), grant.Histories.Single().Amount);
                Assert.Equal(tx.GetHash(), grant.Histories.Single().Transaction);
                Assert.Equal(PropertyGrantType.Grant, grant.Histories.Single().Type);
            }
        }

        [Fact]
        public Task SendAsync_WithNullFrom_ShouldThrow()
        {
            var property = new Property(new PropertyId(3), PropertyType.Indivisible);

            return Assert.ThrowsAsync<ArgumentNullException>(
                "from",
                () => Subject.SendAsync(
                    null,
                    TestAddress.Regtest1,
                    property,
                    PropertyAmount.One,
                    null,
                    CancellationToken.None
                )
            );
        }

        [Fact]
        public Task SendAsync_WithNullTo_ShouldThrow()
        {
            var property = new Property(new PropertyId(3), PropertyType.Indivisible);

            return Assert.ThrowsAsync<ArgumentNullException>(
                "to",
                () => Subject.SendAsync(
                    TestAddress.Regtest1,
                    null,
                    property,
                    PropertyAmount.One,
                    null,
                    CancellationToken.None
                )
            );
        }

        [Fact]
        public Task SendAsync_WithNullProperty_ShouldThrow()
        {
            return Assert.ThrowsAsync<ArgumentNullException>(
                "property",
                () => Subject.SendAsync(
                    TestAddress.Regtest1,
                    TestAddress.Regtest2,
                    null,
                    PropertyAmount.One,
                    null,
                    CancellationToken.None
                )
            );
        }

        [Fact]
        public Task SendAsync_WithInvalidAmount_ShouldThrow()
        {
            var property = new Property(new PropertyId(3), PropertyType.Indivisible);

            return Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                "amount",
                () => Subject.SendAsync(
                    TestAddress.Regtest1,
                    TestAddress.Regtest2,
                    property,
                    PropertyAmount.Zero,
                    null,
                    CancellationToken.None
                )
            );
        }

        [Fact]
        public Task SendAsync_WithInvalidReferenceAmount_ShouldThrow()
        {
            var property = new Property(new PropertyId(3), PropertyType.Indivisible);

            return Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                "referenceAmount",
                () => Subject.SendAsync(
                    TestAddress.Regtest1,
                    TestAddress.Regtest2,
                    property,
                    PropertyAmount.One,
                    Money.Zero,
                    CancellationToken.None
                )
            );
        }

        [Fact]
        public async Task SendAsync_WithNullReferenceAmount_ShouldAutomaticallyChooseOutputAmount()
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

            // Act.
            var tx = await Subject.SendAsync(
                owner,
                receiver,
                property,
                PropertyAmount.One,
                null,
                CancellationToken.None
            );

            await SendTransactionAsync(tx);
            Node.Generate(1);

            // Assert.
            var output = tx.Outputs.Single(o => o.ScriptPubKey.GetDestinationAddress(Network) == receiver);

            Assert.True(output.Value > Money.Zero);

            using (var rpc = await Factory.CreateExodusInformationRpcAsync(CancellationToken.None))
            {
                var (amount, reserved) = await rpc.GetBalanceAsync(receiver, property, CancellationToken.None);

                Assert.Equal(PropertyAmount.One, amount);
                Assert.Equal(PropertyAmount.Zero, reserved);
            }
        }

        [Fact]
        public async Task SendAsync_WithReferenceAmount_ShouldOutputByThatAmount()
        {
            // Arrange.
            var owner = await GenerateNewAddressAsync();
            var receiver = await GenerateNewAddressAsync();
            var issuer = new PropertyIssuer(Factory);

            Node.Generate(101);
            await FundAddressAsync(owner, Money.Coins(10));
            Node.Generate(1);

            await issuer.IssueManagedAsync(owner);
            Node.Generate(1);

            var property = await GetPropertyAsync(issuer.Name);

            await GrantTokensAsync(property, owner, owner, new PropertyAmount(100));
            Node.Generate(1);

            // Act.
            var tx = await Subject.SendAsync(
                owner,
                receiver,
                property,
                PropertyAmount.One,
                Money.Coins(0.01m),
                CancellationToken.None
            );

            await SendTransactionAsync(tx);
            Node.Generate(1);

            // Assert.
            var output = tx.Outputs.Single(o => o.ScriptPubKey.GetDestinationAddress(Network) == receiver);

            Assert.Equal(Money.Coins(0.01m), output.Value);

            using (var rpc = await Factory.CreateExodusInformationRpcAsync(CancellationToken.None))
            {
                var (amount, reserved) = await rpc.GetBalanceAsync(receiver, property, CancellationToken.None);

                Assert.Equal(PropertyAmount.One, amount);
                Assert.Equal(PropertyAmount.Zero, reserved);
            }
        }

        protected override RpcClient CreateSubject()
        {
            return new PropertyManagementRpc(Factory, Node.CreateRPCClient());
        }
    }
}
