using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;
using Ztm.WebApi.AddressPools;
using Ztm.Zcoin.Rpc;

namespace Ztm.WebApi.Tests.AddressPools
{
    public sealed class RpcAddressGeneratorTests
    {
        readonly Mock<IRpcFactory> factory;
        readonly Mock<IWalletRpc> client;
        readonly RpcAddressGenerator subject;

        public RpcAddressGeneratorTests()
        {
            this.client = new Mock<IWalletRpc>();
            this.factory = new Mock<IRpcFactory>();
            this.factory.Setup(f => f.CreateWalletRpcAsync(It.IsAny<CancellationToken>()))
                        .ReturnsAsync(this.client.Object);

            this.subject = new RpcAddressGenerator(this.factory.Object);
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
            this.factory.Verify(
                f => f.CreateWalletRpcAsync(cancellationToken),
                Times.Once()
            );

            this.client.Verify(
                c => c.GetNewAddressAsync(cancellationToken),
                Times.Once()
            );
        }
    }
}
