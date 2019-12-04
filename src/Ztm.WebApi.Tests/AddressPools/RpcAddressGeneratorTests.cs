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
        readonly IZcoinRpcClient client;

        readonly RpcAddressGenerator subject;

        public RpcAddressGeneratorTests()
        {
            this.client = Substitute.For<IZcoinRpcClient>();
            this.subject = new RpcAddressGenerator(this.client);
        }

        [Fact]
        public void Construct_WithNullArgs_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "client",
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
            _ = this.client.Received(1).GetNewAddressAsync(Arg.Is<CancellationToken>(c => c == cancellationToken));
        }
    }
}