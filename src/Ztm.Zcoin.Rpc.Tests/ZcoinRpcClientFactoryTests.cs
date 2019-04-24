using System;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using NBitcoin.RPC;
using Xunit;
using Ztm.Zcoin.Testing;

namespace Ztm.Zcoin.Rpc.Tests
{
    public class ZcoinRpcClientFactoryTests
    {
        [Fact]
        public void Constructor_PassNullForServer_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>("server", () =>
            {
                new ZcoinRpcClientFactory(server: null, type: NetworkType.Regtest, credential: new RPCCredentialString());
            });
        }

        [Fact]
        public void Constructor_PassNullForCredential_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>("credential", () =>
            {
                new ZcoinRpcClientFactory(server: new Uri("http://127.0.0.1"), type: NetworkType.Regtest, credential: null);
            });
        }

        [Fact]
        public async Task CreateRpcClientAsync_WithCorrectParameters_ShouldSuccess()
        {
            using (var nodeBuilder = NodeBuilderFactory.CreateNodeBuilder(GetType().Name))
            {
                var node = nodeBuilder.CreateNode(true);
                var cred = RPCCredentialString.Parse(node.GetRPCAuth());
                var factory = new ZcoinRpcClientFactory(node.RPCUri, node.Network.NetworkType, cred);
                var client = await factory.CreateRpcClientAsync(CancellationToken.None);

                client.Dispose();
            }
        }
    }
}
