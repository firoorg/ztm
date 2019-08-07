using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using NBitcoin.Tests;
using Xunit;
using Ztm.Zcoin.NBitcoin;
using Ztm.Zcoin.NBitcoin.RPC;
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
                this.nodes.ConfigParameters.Add("printtoconsole", "0");

                this.node = this.nodes.CreateNode();
                this.nodes.StartAll();

                this.subject = new ZcoinRpcClient(
                    new ZcoinRPCClient(this.node.GetRPCAuth(), this.node.RPCUri, this.node.Network)
                );
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
        public async Task CreateManagedTokenAsync_PassNullForOwner_ShouldThrow()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                "owner",
                () => this.subject.CreateManagedTokenAsync(
                    null,
                    TokenEcosystem.Main,
                    TokenType.Divisible,
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
        public async Task CreateManagedTokenAsync_PassInvalidTokenId_ShouldThrow()
        {
            var owner = await this.subject.GetNewAddressAsync(CancellationToken.None);

            await Assert.ThrowsAsync<ArgumentException>(
                "currentId",
                () => this.subject.CreateManagedTokenAsync(
                    owner,
                    TokenEcosystem.Main,
                    TokenType.Divisible,
                    default(TokenId),
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
        public async Task CreateManagedTokenAsync_PassNullForCategory_ShouldThrow()
        {
            var owner = await this.subject.GetNewAddressAsync(CancellationToken.None);

            await Assert.ThrowsAsync<ArgumentNullException>(
                "category",
                () => this.subject.CreateManagedTokenAsync(
                    owner,
                    TokenEcosystem.Main,
                    TokenType.Divisible,
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
        public async Task CreateManagedTokenAsync_PassNullForSubcategory_ShouldThrow()
        {
            var owner = await this.subject.GetNewAddressAsync(CancellationToken.None);

            await Assert.ThrowsAsync<ArgumentNullException>(
                "subcategory",
                () => this.subject.CreateManagedTokenAsync(
                    owner,
                    TokenEcosystem.Main,
                    TokenType.Divisible,
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
        public async Task CreateManagedTokenAsync_PassNullForName_ShouldThrow()
        {
            var owner = await this.subject.GetNewAddressAsync(CancellationToken.None);

            await Assert.ThrowsAsync<ArgumentNullException>(
                "name",
                () => this.subject.CreateManagedTokenAsync(
                    owner,
                    TokenEcosystem.Main,
                    TokenType.Divisible,
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
        public async Task CreateManagedTokenAsync_PassNullForUrl_ShouldThrow()
        {
            var owner = await this.subject.GetNewAddressAsync(CancellationToken.None);

            await Assert.ThrowsAsync<ArgumentNullException>(
                "url",
                () => this.subject.CreateManagedTokenAsync(
                    owner,
                    TokenEcosystem.Main,
                    TokenType.Divisible,
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
        public async Task CreateManagedTokenAsync_PassNullForDescription_ShouldThrow()
        {
            var owner = await this.subject.GetNewAddressAsync(CancellationToken.None);

            await Assert.ThrowsAsync<ArgumentNullException>(
                "description",
                () => this.subject.CreateManagedTokenAsync(
                    owner,
                    TokenEcosystem.Main,
                    TokenType.Divisible,
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
        public async Task CreateManagedTokenAsync_WithValidParams_ShouldSuccess()
        {
            // Arrange.
            var owner = await this.subject.GetNewAddressAsync(CancellationToken.None);

            this.node.Generate(101);
            await this.subject.SendToAddressAsync(owner, Money.Coins(30), null, null, false, CancellationToken.None);
            this.node.Generate(1);

            // Act.
            var tx = await this.subject.CreateManagedTokenAsync(
                owner,
                TokenEcosystem.Main,
                TokenType.Indivisible,
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
            var tokens = await this.subject.ListTokensAsync(CancellationToken.None);

            Assert.Equal(3, tokens.Count());
            Assert.Equal(3U, tokens.Last().Id.Value);
            Assert.Equal("Satang Corporation", tokens.Last().Name);
            Assert.Equal("Company", tokens.Last().Category);
            Assert.Equal("Private", tokens.Last().Subcategory);
            Assert.Equal("https://satang.com", tokens.Last().Url);
            Assert.Equal("Provides cryptocurrency solutions.", tokens.Last().Description);
            Assert.Equal(TokenType.Indivisible, tokens.Last().Type);
        }

        [Fact]
        public async Task GetNewAddressAsync_WithWalletEnableNode_ShouldSuccess()
        {
            var address = await this.subject.GetNewAddressAsync(CancellationToken.None);

            Assert.NotNull(address);
        }

        [Fact]
        public async Task GetTokenGrantsAsync_WithInvalidId_ShouldThrow()
        {
            await Assert.ThrowsAsync<ArgumentException>(
                "token",
                () => this.subject.GetTokenGrantsAsync(default(TokenId), CancellationToken.None)
            );
        }

        [Fact]
        public async Task GetTokenGrantsAsync_WithEmptyHistories_ShouldSuccess()
        {
            // Arrange.
            var owner = await this.subject.GetNewAddressAsync(CancellationToken.None);

            this.node.Generate(101);
            await this.subject.SendToAddressAsync(owner, Money.Coins(30), null, null, false, CancellationToken.None);
            this.node.Generate(1);

            var tx = await this.subject.CreateManagedTokenAsync(
                owner,
                TokenEcosystem.Main,
                TokenType.Indivisible,
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
            var info = await this.subject.GetTokenGrantsAsync(3, CancellationToken.None);

            // Assert.
            Assert.Equal(3U, info.Id.Value);
            Assert.Equal("Satang Corporation", info.Name);
            Assert.Equal(owner, info.Issuer);
            Assert.Equal(tx.GetHash(), info.CreationTransaction);
            Assert.Null(info.TotalTokens);
            Assert.Empty(info.Histories);
        }

        [Fact]
        public async Task GrantTokensAsync_WithInvalidId_ShouldThrow()
        {
            var owner = await this.subject.GetNewAddressAsync(CancellationToken.None);
            var to = BitcoinAddress.Create("TG3Pnw5xPZQS8JXMVa3F9WjUFfUqXKsqAz", this.node.Network);

            await Assert.ThrowsAsync<ArgumentException>(
                "id",
                () => this.subject.GrantTokensAsync(
                    default(TokenId),
                    owner,
                    to,
                    TokenAmount.Indivisible(100),
                    null,
                    CancellationToken.None
                )
            );
        }

        [Fact]
        public async Task GrantTokensAsync_WithNullFrom_ShouldThrow()
        {
            var to = BitcoinAddress.Create("TG3Pnw5xPZQS8JXMVa3F9WjUFfUqXKsqAz", this.node.Network);

            await Assert.ThrowsAsync<ArgumentNullException>(
                "from",
                () => this.subject.GrantTokensAsync(
                    3,
                    null,
                    to,
                    TokenAmount.Indivisible(100),
                    null,
                    CancellationToken.None
                )
            );
        }

        [Fact]
        public async Task GrantTokensAsync_WithNullTo_ShouldThrow()
        {
            var owner = await this.subject.GetNewAddressAsync(CancellationToken.None);

            await Assert.ThrowsAsync<ArgumentNullException>(
                "to",
                () => this.subject.GrantTokensAsync(
                    3,
                    owner,
                    null,
                    TokenAmount.Indivisible(100),
                    null,
                    CancellationToken.None
                )
            );
        }

        [Fact]
        public async Task GrantTokensAsync_WithInvalidAmount_ShouldThrow()
        {
            var owner = await this.subject.GetNewAddressAsync(CancellationToken.None);
            var to = BitcoinAddress.Create("TG3Pnw5xPZQS8JXMVa3F9WjUFfUqXKsqAz", this.node.Network);

            await Assert.ThrowsAsync<ArgumentException>(
                "amount",
                () => this.subject.GrantTokensAsync(
                    3,
                    owner,
                    to,
                    default(TokenAmount),
                    null,
                    CancellationToken.None
                )
            );
        }

        [Fact]
        public async Task GrantTokensAsync_WithValidParameters_ShouldSuccess()
        {
            // Arrange.
            var owner = await this.subject.GetNewAddressAsync(CancellationToken.None);

            this.node.Generate(101);
            await this.subject.SendToAddressAsync(owner, Money.Coins(30), null, null, false, CancellationToken.None);
            this.node.Generate(1);

            var createTx = await this.subject.CreateManagedTokenAsync(
                owner,
                TokenEcosystem.Main,
                TokenType.Indivisible,
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

            // Act.
            var grantTx = await this.subject.GrantTokensAsync(
                3,
                owner,
                owner,
                TokenAmount.Indivisible(1000),
                null,
                CancellationToken.None);

            await this.subject.SendRawTransactionAsync(grantTx, CancellationToken.None);

            this.node.Generate(1);

            // Assert.
            var info = await this.subject.GetTokenGrantsAsync(3, CancellationToken.None);

            Assert.Equal(3U, info.Id.Value);
            Assert.Equal("Satang Corporation", info.Name);
            Assert.Equal(owner, info.Issuer);
            Assert.Equal(createTx.GetHash(), info.CreationTransaction);
            Assert.Equal("1000", info.TotalTokens.Value.ToString());
            Assert.Single(info.Histories);

            Assert.Equal(TokenGrantType.Grant, info.Histories.First().Type);
            Assert.Equal(grantTx.GetHash(), info.Histories.First().Transaction);
            Assert.Equal("1000", info.Histories.First().Amount.ToString());
        }

        [Fact]
        public async Task ListTokensAsync_WithExodusEnabled_ShouldSuccess()
        {
            var tokens = await this.subject.ListTokensAsync(CancellationToken.None);

            Assert.Equal(2, tokens.Count());

            Assert.Equal(1U, tokens.First().Id.Value);
            Assert.Equal("Exodus", tokens.First().Name);
            Assert.Equal("N/A", tokens.First().Category);
            Assert.Equal("N/A", tokens.First().Subcategory);
            Assert.Equal("https://www.zcoin.io", tokens.First().Url);
            Assert.Equal("Exodus serve as the binding between Zcoin, smart properties and contracts created on the Exodus Layer.", tokens.First().Description);
            Assert.Equal(TokenType.Divisible, tokens.First().Type);

            Assert.Equal(2U, tokens.Last().Id.Value);
            Assert.Equal("Test Exodus", tokens.Last().Name);
            Assert.Equal("N/A", tokens.Last().Category);
            Assert.Equal("N/A", tokens.Last().Subcategory);
            Assert.Equal("https://www.zcoin.io", tokens.Last().Url);
            Assert.Equal("Test Exodus serve as the binding between Zcoin, smart properties and contracts created on the Exodus Layer.", tokens.Last().Description);
            Assert.Equal(TokenType.Divisible, tokens.Last().Type);
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
