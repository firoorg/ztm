using System;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Xunit;
using Ztm.WebApi.AddressPools;
using Ztm.Zcoin.Rpc;

namespace Ztm.WebApi.Tests.AddressPools
{
    public sealed class RpcAddressGeneratorTests
    {
        readonly IZcoinRpcClientFactory factory;
        readonly IZcoinRpcClient client;
        readonly RpcAddressGenerator subject;

        public RpcAddressGeneratorTests()
        {
            this.client = Substitute.For<IZcoinRpcClient>();
            this.factory = Substitute.For<IZcoinRpcClientFactory>();
            this.factory.CreateRpcClientAsync(Arg.Any<CancellationToken>()).Returns(this.client);

            this.subject = new RpcAddressGenerator(this.factory);
        }

        [Fact]
        public void Construct_WithNull_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "factory",
                () => new RpcAddressGenerator(null)
            );
        }

        [Fact]
        public async Task GenerateAsync_ShouldSuccess()
        {
            // Arrange.
            var cancellationToken = new CancellationToken(false);

            // Act.
            await this.subject.GenerateAsync(cancellationToken);

            // Assert.
            _ = this.factory.Received(1).CreateRpcClientAsync(Arg.Is<CancellationToken>(c => c == cancellationToken));
            _ = this.client.Received(1).GetNewAddressAsync(Arg.Is<CancellationToken>(c => c == cancellationToken));
        }
    }
}