using System;
using System.Threading.Tasks;
using NBitcoin.Tests;
using Xunit;
using Ztm.Zcoin.NBitcoin.RPC;
using Ztm.Zcoin.Testing;

namespace Ztm.Zcoin.NBitcoin.Tests
{
    public class RpcTests : IDisposable
    {
        readonly NodeBuilder builder;
        readonly CoreNode node;
        readonly ZcoinRPCClient rpc;

        public RpcTests()
        {
            this.builder = NodeBuilderFactory.CreateNodeBuilder(GetType().Name);
            this.node = this.builder.CreateNode(true);
            this.rpc = new ZcoinRPCClient(node.GetRPCAuth(), node.RPCUri, node.Network); node.CreateRPCClient();
        }

        public void Dispose()
        {
            this.builder.Dispose();
        }

        [Fact]
        public async Task CompatibilityCheck()
        {
            var caps = await this.rpc.ScanRPCCapabilitiesAsync();

            Assert.Equal(130707, caps.Version);
            Assert.True(caps.SupportGetNetworkInfo);
            Assert.False(caps.SupportScanUTXOSet);
            Assert.False(caps.SupportSignRawTransactionWith);
            Assert.True(caps.SupportEstimateSmartFee);
            Assert.True(caps.SupportGenerateToAddress);
            Assert.False(caps.SupportSegwit);
        }
    }
}
