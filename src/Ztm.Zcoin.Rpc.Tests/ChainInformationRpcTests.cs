using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using Xunit;
using Ztm.Zcoin.NBitcoin;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.Zcoin.Rpc.Tests
{
    public sealed class ChainInformationRpcTests : RpcClientTesting
    {
        new ChainInformationRpc Subject => (ChainInformationRpc)base.Subject;

        [Fact]
        public Task GetBlockAsync_WithNullHash_ShouldThrow()
        {
            return Assert.ThrowsAsync<ArgumentNullException>(
                "hash",
                () => Subject.GetBlockAsync(null, CancellationToken.None)
            );
        }

        [Fact]
        public async Task GetBlockAsync_WithGenesisHash_ShouldReturnGenesisBlock()
        {
            var genesis = Network.GenesisHash;

            var block = await Subject.GetBlockAsync(genesis, CancellationToken.None);

            Assert.Equal(genesis, block.GetHash());
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-2)]
        public Task GetBlockAsync_WithInvalidHeight_ShouldThrow(int height)
        {
            return Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                "height",
                () => Subject.GetBlockAsync(height, CancellationToken.None)
            );
        }

        [Fact]
        public async Task GetBlockAsync_WithGenesisHeight_ShouldReturnGenesisBlock()
        {
            var block = await Subject.GetBlockAsync(0, CancellationToken.None);

            Assert.Equal(Network.GenesisHash, block.GetHash());
        }

        [Fact]
        public async Task GetBlockAsync_WithExodusTransaction_ShouldPopulateRelatedProperties()
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

            await SendTokensAsync(property, owner, receiver, new PropertyAmount(1));

            var hash = Node.Generate(1).Single();
            var height = 105;

            // Act.
            var result1 = await Subject.GetBlockAsync(hash, CancellationToken.None);
            var result2 = await Subject.GetBlockAsync(height, CancellationToken.None);

            // Assert.
            Assert.Equal(result1.GetHash(), result2.GetHash());
            Assert.Equal(2, result1.Transactions.Count);

            var exodus1 = result1.Transactions[0].GetExodusTransaction();
            var exodus2 = result1.Transactions[1].GetExodusTransaction();

            Assert.Null(exodus1);
            Assert.NotNull(exodus2);

            Assert.Equal(SimpleSendV0.StaticId, exodus2.Id);
        }

        [Fact]
        public Task GetBlockHeaderAsync_WithNullHash_ShouldThrow()
        {
            return Assert.ThrowsAsync<ArgumentNullException>(
                "hash",
                () => Subject.GetBlockHeaderAsync(null, CancellationToken.None)
            );
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-2)]
        public Task GetBlockHeaderAsync_WithInvalidHeight_ShouldThrow(int height)
        {
            return Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                "height",
                () => Subject.GetBlockHeaderAsync(height, CancellationToken.None)
            );
        }

        [Fact]
        public async Task GetBlockHeaderAsync_WithGenesisHash_ShouldReturnGenesisHeader()
        {
            var genesis = Network.GenesisHash;

            var header = await Subject.GetBlockHeaderAsync(genesis, CancellationToken.None);

            Assert.Equal(genesis, header.GetHash());
        }

        [Fact]
        public async Task GetBlockHeaderAsync_WithGenesisHeight_ShouldReturnGenesisHeader()
        {
            var header = await Subject.GetBlockHeaderAsync(0, CancellationToken.None);

            Assert.Equal(Network.GenesisHash, header.GetHash());
        }

        [Fact]
        public async Task GetChainInfoAsync_WhenInvoke_ShouldReturnCurrentChainInfo()
        {
            // Arrange.
            var blocks = Node.Generate(3);

            // Act.
            var info = await Subject.GetChainInfoAsync(CancellationToken.None);

            // Assert.
            Assert.Equal(NetworkType.Regtest, info.Chain.NetworkType);
            Assert.Equal(3U, info.Blocks);
            Assert.Equal(3U, info.Headers);
            Assert.Equal(blocks.Last(), info.BestBlockHash);
            Assert.Equal(0U, info.Difficulty);
            Assert.NotEqual(0U, info.MedianTime);
            Assert.NotEqual(default(float), info.VerificationProgress);
            Assert.False(info.InitialBlockDownload);
            Assert.NotEqual(uint256.Zero, info.ChainWork);
            Assert.Equal(0U, info.SizeOnDisk);
            Assert.False(info.Pruned);
            Assert.NotEmpty(info.SoftForks);
            Assert.NotEmpty(info.Bip9SoftForks);

            Assert.NotEmpty(info.SoftForks[0].Bip);
            Assert.NotEqual(0, info.SoftForks[0].Version);

            Assert.NotEmpty(info.Bip9SoftForks[0].Name);
            Assert.NotEmpty(info.Bip9SoftForks[0].Status);
            Assert.NotEqual(default(DateTimeOffset), info.Bip9SoftForks[0].StartTime);
            Assert.NotEqual(default(DateTimeOffset), info.Bip9SoftForks[0].Timeout);
        }

        protected override RpcClient CreateSubject()
        {
            return new ChainInformationRpc(Factory, Node.CreateRPCClient());
        }
    }
}
