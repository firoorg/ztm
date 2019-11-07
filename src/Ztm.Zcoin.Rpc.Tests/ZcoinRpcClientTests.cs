using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using NBitcoin.Tests;
using Xunit;
using Ztm.Zcoin.NBitcoin.Exodus;
using Ztm.Zcoin.Testing;

namespace Ztm.Zcoin.Rpc.Tests
{
    public sealed class ZcoinRpcClientTests : IDisposable
    {
        readonly NodeBuilder nodes;
        readonly CoreNode node;
        readonly ZcoinRpcClient subject;

        public ZcoinRpcClientTests()
        {
            this.nodes = NodeBuilderFactory.CreateNodeBuilder(GetType().FullName);

            try
            {
                this.nodes.ConfigParameters.Add("autocommit", "0");
                this.nodes.ConfigParameters.Add("dandelion", "0");
                this.nodes.ConfigParameters.Add("exodus", "1");

                this.node = this.nodes.CreateNode();
                this.nodes.StartAll();

                this.subject = new ZcoinRpcClient(this.node.CreateRPCClient());
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
        public async Task CreateManagedPropertyAsync_PassNullForOwner_ShouldThrow()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                "owner",
                () => this.subject.CreateManagedPropertyAsync(
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
        public async Task CreateManagedPropertyAsync_PassNullForCategory_ShouldThrow()
        {
            var owner = await this.subject.GetNewAddressAsync(CancellationToken.None);

            await Assert.ThrowsAsync<ArgumentNullException>(
                "category",
                () => this.subject.CreateManagedPropertyAsync(
                    owner,
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
        public async Task CreateManagedPropertyAsync_PassNullForSubcategory_ShouldThrow()
        {
            var owner = await this.subject.GetNewAddressAsync(CancellationToken.None);

            await Assert.ThrowsAsync<ArgumentNullException>(
                "subcategory",
                () => this.subject.CreateManagedPropertyAsync(
                    owner,
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
        public async Task CreateManagedPropertyAsync_PassNullForName_ShouldThrow()
        {
            var owner = await this.subject.GetNewAddressAsync(CancellationToken.None);

            await Assert.ThrowsAsync<ArgumentNullException>(
                "name",
                () => this.subject.CreateManagedPropertyAsync(
                    owner,
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
        public async Task CreateManagedPropertyAsync_PassNullForUrl_ShouldThrow()
        {
            var owner = await this.subject.GetNewAddressAsync(CancellationToken.None);

            await Assert.ThrowsAsync<ArgumentNullException>(
                "url",
                () => this.subject.CreateManagedPropertyAsync(
                    owner,
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
        public async Task CreateManagedPropertyAsync_PassNullForDescription_ShouldThrow()
        {
            var owner = await this.subject.GetNewAddressAsync(CancellationToken.None);

            await Assert.ThrowsAsync<ArgumentNullException>(
                "description",
                () => this.subject.CreateManagedPropertyAsync(
                    owner,
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
        public async Task CreateManagedPropertyAsync_WithValidParams_ShouldSuccess()
        {
            // Arrange.
            var owner = await this.subject.GetNewAddressAsync(CancellationToken.None);

            this.node.Generate(101);
            await this.subject.SendToAddressAsync(owner, Money.Coins(30), null, null, false, CancellationToken.None);
            this.node.Generate(1);

            // Act.
            var tx = await this.subject.CreateManagedPropertyAsync(
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

            await this.subject.SendRawTransactionAsync(tx, CancellationToken.None);

            this.node.Generate(1);

            // Assert.
            var props = await this.subject.ListPropertiesAsync(CancellationToken.None);

            Assert.Equal(3, props.Count());
            Assert.Equal(3, props.Last().Id.Value);
            Assert.Equal("Satang Corporation", props.Last().Name);
            Assert.Equal("Company", props.Last().Category);
            Assert.Equal("Private", props.Last().Subcategory);
            Assert.Equal("https://satang.com", props.Last().Url);
            Assert.Equal("Provides cryptocurrency solutions.", props.Last().Description);
            Assert.Equal(PropertyType.Indivisible, props.Last().Type);
        }

        [Fact]
        public async Task GetNewAddressAsync_WithWalletEnableNode_ShouldSuccess()
        {
            var address = await this.subject.GetNewAddressAsync(CancellationToken.None);

            Assert.NotNull(address);
        }

        [Fact]
        public async Task GetPropertyGrantsAsync_WithNullProperty_ShouldThrow()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                "property",
                () => this.subject.GetPropertyGrantsAsync(null, CancellationToken.None)
            );
        }

        [Fact]
        public async Task GetPropertyGrantsAsync_WithEmptyHistories_ShouldSuccess()
        {
            // Arrange.
            var owner = await this.subject.GetNewAddressAsync(CancellationToken.None);

            this.node.Generate(101);
            await this.subject.SendToAddressAsync(owner, Money.Coins(30), null, null, false, CancellationToken.None);
            this.node.Generate(1);

            var tx = await this.subject.CreateManagedPropertyAsync(
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

            await this.subject.SendRawTransactionAsync(tx, CancellationToken.None);

            this.node.Generate(1);

            // Act.
            var info = await this.subject.GetPropertyGrantsAsync(
                new Property(new PropertyId(3), PropertyType.Indivisible),
                CancellationToken.None
            );

            // Assert.
            Assert.Equal(3, info.Id.Value);
            Assert.Equal("Satang Corporation", info.Name);
            Assert.Equal(owner, info.Issuer);
            Assert.Equal(tx.GetHash(), info.CreationTransaction);
            Assert.Equal(PropertyAmount.Zero, info.TotalTokens);
            Assert.Empty(info.Histories);
        }

        [Fact]
        public async Task GrantPropertyAsync_WithNullProperty_ShouldThrow()
        {
            var owner = await this.subject.GetNewAddressAsync(CancellationToken.None);
            var to = BitcoinAddress.Create("TG3Pnw5xPZQS8JXMVa3F9WjUFfUqXKsqAz", this.node.Network);

            await Assert.ThrowsAsync<ArgumentNullException>(
                "property",
                () => this.subject.GrantPropertyAsync(
                    null,
                    owner,
                    to,
                    new PropertyAmount(100),
                    null,
                    CancellationToken.None
                )
            );
        }

        [Fact]
        public async Task GrantPropertyAsync_WithNullFrom_ShouldThrow()
        {
            var to = BitcoinAddress.Create("TG3Pnw5xPZQS8JXMVa3F9WjUFfUqXKsqAz", this.node.Network);

            await Assert.ThrowsAsync<ArgumentNullException>(
                "from",
                () => this.subject.GrantPropertyAsync(
                    new Property(new PropertyId(3), PropertyType.Indivisible),
                    null,
                    to,
                    new PropertyAmount(100),
                    null,
                    CancellationToken.None
                )
            );
        }

        [Fact]
        public async Task GrantPropertyAsync_WithNullTo_ShouldThrow()
        {
            var owner = await this.subject.GetNewAddressAsync(CancellationToken.None);

            await Assert.ThrowsAsync<ArgumentNullException>(
                "to",
                () => this.subject.GrantPropertyAsync(
                    new Property(new PropertyId(3), PropertyType.Indivisible),
                    owner,
                    null,
                    new PropertyAmount(100),
                    null,
                    CancellationToken.None
                )
            );
        }

        [Fact]
        public async Task GrantPropertyAsync_WithInvalidAmount_ShouldThrow()
        {
            var owner = await this.subject.GetNewAddressAsync(CancellationToken.None);
            var to = BitcoinAddress.Create("TG3Pnw5xPZQS8JXMVa3F9WjUFfUqXKsqAz", this.node.Network);

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                "amount",
                () => this.subject.GrantPropertyAsync(
                    new Property(new PropertyId(3), PropertyType.Indivisible),
                    owner,
                    to,
                    PropertyAmount.Zero,
                    null,
                    CancellationToken.None
                )
            );
        }

        [Fact]
        public async Task GrantPropertyAsync_WithValidParameters_ShouldSuccess()
        {
            // Arrange.
            var owner = await this.subject.GetNewAddressAsync(CancellationToken.None);

            this.node.Generate(101);
            await this.subject.SendToAddressAsync(owner, Money.Coins(30), null, null, false, CancellationToken.None);
            this.node.Generate(1);

            var createTx = await this.subject.CreateManagedPropertyAsync(
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

            await this.subject.SendRawTransactionAsync(createTx, CancellationToken.None);

            this.node.Generate(1);

            var property = new Property(new PropertyId(3), PropertyType.Indivisible);

            // Act.
            var grantTx = await this.subject.GrantPropertyAsync(
                property,
                owner,
                owner,
                new PropertyAmount(1000),
                null,
                CancellationToken.None);

            await this.subject.SendRawTransactionAsync(grantTx, CancellationToken.None);

            this.node.Generate(1);

            // Assert.
            var info = await this.subject.GetPropertyGrantsAsync(property, CancellationToken.None);

            Assert.Equal(3, info.Id.Value);
            Assert.Equal("Satang Corporation", info.Name);
            Assert.Equal(owner, info.Issuer);
            Assert.Equal(createTx.GetHash(), info.CreationTransaction);
            Assert.Equal("1000", info.TotalTokens.ToString(property.Type));
            Assert.Single(info.Histories);

            Assert.Equal(PropertyGrantType.Grant, info.Histories.First().Type);
            Assert.Equal(grantTx.GetHash(), info.Histories.First().Transaction);
            Assert.Equal("1000", info.Histories.First().Amount.ToString(property.Type));
        }

        [Fact]
        public async Task ListPropertiesAsync_WithExodusEnabled_ShouldSuccess()
        {
            var props = await this.subject.ListPropertiesAsync(CancellationToken.None);

            Assert.Equal(2, props.Count());

            Assert.Equal(1, props.First().Id.Value);
            Assert.Equal("Exodus", props.First().Name);
            Assert.Equal("N/A", props.First().Category);
            Assert.Equal("N/A", props.First().Subcategory);
            Assert.Equal("https://www.zcoin.io", props.First().Url);
            Assert.Equal("Exodus serve as the binding between Zcoin, smart properties and contracts created on the Exodus Layer.", props.First().Description);
            Assert.Equal(PropertyType.Divisible, props.First().Type);

            Assert.Equal(2, props.Last().Id.Value);
            Assert.Equal("Test Exodus", props.Last().Name);
            Assert.Equal("N/A", props.Last().Category);
            Assert.Equal("N/A", props.Last().Subcategory);
            Assert.Equal("https://www.zcoin.io", props.Last().Url);
            Assert.Equal("Test Exodus serve as the binding between Zcoin, smart properties and contracts created on the Exodus Layer.", props.Last().Description);
            Assert.Equal(PropertyType.Divisible, props.Last().Type);
        }

        [Fact]
        public async Task SendRawTransactionAsync_WithNullTx_ShouldThrow()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                "tx",
                () => this.subject.SendRawTransactionAsync(null, CancellationToken.None)
            );
        }

        [Fact]
        public async Task SendToAddressAsync_WithNullAddress_ShouldThrow()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                "address",
                () => this.subject.SendToAddressAsync(null, Money.Coins(1), null, null, false, CancellationToken.None)
            );
        }

        [Fact]
        public async Task SendToAddressAsync_WithNullAmount_ShouldThrow()
        {
            var to = BitcoinAddress.Create("TG3Pnw5xPZQS8JXMVa3F9WjUFfUqXKsqAz", this.node.Network);

            await Assert.ThrowsAsync<ArgumentNullException>(
                "amount",
                () => this.subject.SendToAddressAsync(to, null, null, null, false, CancellationToken.None)
            );
        }
    }
}
