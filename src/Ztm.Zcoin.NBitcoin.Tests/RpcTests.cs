using System;
using System.Threading.Tasks;
using NBitcoin;
using NBitcoin.RPC;
using NBitcoin.Tests;
using Xunit;
using Ztm.Zcoin.Testing;

namespace Ztm.Zcoin.NBitcoin.Tests
{
    public sealed class RpcTests : IDisposable
    {
        readonly NodeBuilder builder;
        readonly CoreNode node;
        readonly RPCClient rpc;

        public RpcTests()
        {
            this.builder = NodeBuilderFactory.CreateNodeBuilder(GetType());
            this.node = this.builder.CreateNode(true);
            this.rpc = node.CreateRPCClient();
        }

        public void Dispose()
        {
            this.builder.Dispose();
        }

        [Fact]
        public async Task ScanRPCCapabilitiesAsync_WithCorrectState_ShouldSuccess()
        {
            var caps = await this.rpc.ScanRPCCapabilitiesAsync();

            Assert.True(caps.Version > 0);
            Assert.True(caps.SupportGetNetworkInfo);
            Assert.False(caps.SupportScanUTXOSet);
            Assert.False(caps.SupportSignRawTransactionWith);
            Assert.True(caps.SupportEstimateSmartFee);
            Assert.True(caps.SupportGenerateToAddress);
            Assert.False(caps.SupportSegwit);
        }

        [Fact]
        public async Task GetBlockHeaderAsync_WithBlockHash_ShouldSuccess()
        {
            var hash = new uint256("a42b98f04cc2916e8adfb5d9db8a2227c4629bc205748ed2f33180b636ee885b");
            var header = await this.rpc.GetBlockHeaderAsync(hash);

            Assert.Equal(hash, header.GetHash());
            Assert.Equal(this.node.Network.GenesisHash, header.GetHash());
        }

        [Fact]
        public async Task GetBlockHeaderAsync_WithBlockHeight_ShouldSuccess()
        {
            var header = await this.rpc.GetBlockHeaderAsync(0);

            Assert.Equal(this.node.Network.GenesisHash, header.GetHash());
        }

        [Fact]
        public async Task GetBlockAsync_WithBlockHash_ShouldSuccess()
        {
            var hash = new uint256("a42b98f04cc2916e8adfb5d9db8a2227c4629bc205748ed2f33180b636ee885b");
            var block = await this.rpc.GetBlockAsync(hash);

            Assert.Equal(hash, block.GetHash());
            Assert.Equal(this.node.Network.GenesisHash, block.GetHash());
            Assert.Single(block.Transactions);
            Assert.True(block.Transactions[0].IsCoinBase);
            Assert.Equal(new uint256("25b361d60bc7a66b311e72389bf5d9add911c735102bcb6425f63aceeff5b7b8"), block.Transactions[0].GetHash());
        }

        [Fact]
        public async Task GetBlockAsync_WithBlockHeight_ShouldSuccess()
        {
            var block = await this.rpc.GetBlockAsync(0);

            Assert.Equal(this.node.Network.GenesisHash, block.GetHash());
            Assert.Single(block.Transactions);
            Assert.True(block.Transactions[0].IsCoinBase);
            Assert.Equal(new uint256("25b361d60bc7a66b311e72389bf5d9add911c735102bcb6425f63aceeff5b7b8"), block.Transactions[0].GetHash());
        }
    }
}
