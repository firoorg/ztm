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

                node.Generate(3);

                using (var client = await factory.CreateRpcClientAsync(CancellationToken.None))
                {
                    var info = await client.GetBlockchainInfoAsync(CancellationToken.None);

                    Assert.Equal(NetworkType.Regtest, info.Chain.NetworkType);
                    Assert.Equal(3UL, info.Blocks);
                    Assert.Equal(3UL, info.Headers);
                    Assert.NotEqual(uint256.Zero, info.BestBlockHash);
                    Assert.NotEqual(0UL, info.MedianTime);
                    Assert.False(info.InitialBlockDownload);
                    Assert.NotEqual(uint256.Zero, info.ChainWork);
                    Assert.Equal(0UL, info.SizeOnDisk);
                    Assert.NotEmpty(info.SoftForks);
                    Assert.NotEmpty(info.Bip9SoftForks);

                    Assert.NotEmpty(info.SoftForks[0].Bip);
                    Assert.NotEqual(0, info.SoftForks[0].Version);

                    Assert.NotEmpty(info.Bip9SoftForks[0].Name);
                    Assert.NotEmpty(info.Bip9SoftForks[0].Status);
                    Assert.NotEqual(default(DateTimeOffset), info.Bip9SoftForks[0].StartTime);
                    Assert.NotEqual(default(DateTimeOffset), info.Bip9SoftForks[0].Timeout);
                    Assert.Equal(0UL, info.Bip9SoftForks[0].SinceHeight);
                }
            }
        }
    }
}
