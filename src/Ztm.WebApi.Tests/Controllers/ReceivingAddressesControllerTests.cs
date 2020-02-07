using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using Ztm.Testing;
using Ztm.WebApi.AddressPools;
using Ztm.WebApi.Controllers;
using Ztm.WebApi.Models;

namespace Ztm.WebApi.Tests.Controllers
{
    public sealed class ReceivingAddressesControllerTests
    {
        readonly Mock<IReceivingAddressPool> pool;
        readonly ReceivingAddressesController subject;

        public ReceivingAddressesControllerTests()
        {
            this.pool = new Mock<IReceivingAddressPool>();
            this.subject = new ReceivingAddressesController(this.pool.Object);
        }

        [Fact]
        public void Constructor_WithNullPool_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>("pool", () => new ReceivingAddressesController(null));
        }

        [Fact]
        public Task PostAsync_WithValidArgs_ShouldGenerateNewAddress()
        {
            return AsynchronousTesting.WithCancellationTokenAsync(async cancellationToken =>
            {
                // Arrange.
                var address = new ReceivingAddress(
                    Guid.NewGuid(),
                    TestAddress.Regtest1,
                    false,
                    new Collection<ReceivingAddressReservation>());

                var request = new CreateReceivingAddressesRequest();

                this.pool
                    .Setup(p => p.GenerateAddressAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(address);

                // Act.
                var result = await this.subject.PostAsync(request, cancellationToken);

                // Assert.
                var ok = Assert.IsType<OkObjectResult>(result);
                var response = Assert.IsType<CreateReceivingAddressesResponse>(ok.Value);

                Assert.Equal(address.Address, response.Address);

                this.pool.Verify(p => p.GenerateAddressAsync(cancellationToken), Times.Once());
            });
        }
    }
}
