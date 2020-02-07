using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;
using Ztm.Testing;
using Ztm.WebApi.AddressPools;

namespace Ztm.WebApi.Tests.AddressPools
{
    public sealed class ReceivingAddressPoolTests
    {
        readonly Mock<IAddressGenerator> generator;
        readonly Mock<IReceivingAddressRepository> repository;
        readonly Mock<IAddressChoser> choser;
        readonly ReceivingAddressPool subject;

        public ReceivingAddressPoolTests()
        {
            this.generator = new Mock<IAddressGenerator>();
            this.repository = new Mock<IReceivingAddressRepository>();
            this.choser = new Mock<IAddressChoser>();

            this.subject = new ReceivingAddressPool(this.generator.Object, this.repository.Object, this.choser.Object);
        }

        [Fact]
        public void Construct_WithArgs_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "generator",
                () => new ReceivingAddressPool(null, this.repository.Object, this.choser.Object));

            Assert.Throws<ArgumentNullException>(
                "repository",
                () => new ReceivingAddressPool(this.generator.Object, null, this.choser.Object));

            Assert.Throws<ArgumentNullException>(
                "choser",
                () => new ReceivingAddressPool(this.generator.Object, this.repository.Object, null));
        }

        [Fact]
        public Task GenerateAddressAsync_WhenInvoke_ShouldReturnGeneratedAddress()
        {
            return AsynchronousTesting.WithCancellationTokenAsync(async cancellationToken =>
            {
                // Arrange.
                var address = new ReceivingAddress(
                    Guid.NewGuid(),
                    TestAddress.Regtest1,
                    false,
                    new Collection<ReceivingAddressReservation>());

                this.generator
                    .Setup(g => g.GenerateAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(address.Address);

                this.repository
                    .Setup(r => r.AddAsync(address.Address, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(address);

                // Act.
                var result = await this.subject.GenerateAddressAsync(cancellationToken);

                // Assert.
                Assert.Same(address, result);

                this.generator.Verify(g => g.GenerateAsync(cancellationToken), Times.Once());
                this.repository.Verify(r => r.AddAsync(address.Address, CancellationToken.None), Times.Once());
            });
        }

        [Fact]
        public async Task TryLockAddressAsync_AndNoAddress_ShouldReturnNull()
        {
            // Act.
            var recv = await this.subject.TryLockAddressAsync(CancellationToken.None);

            // Assert.
            Assert.Null(recv);
        }

        [Fact]
        public async Task TryLockAddressAsync_AndAllAddressesAreUnavailable_ShouldReturnNull()
        {
            // Arrange.
            this.repository
                .Setup(r => r.ListAsync(AddressFilter.Available, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Enumerable.Empty<ReceivingAddress>());

            // Act.
            var reservation = await this.subject.TryLockAddressAsync(CancellationToken.None);

            // Assert.
            Assert.Null(reservation);
        }

        [Fact]
        public Task TryLockAddressAsync_AndHaveAnAvailable_ShouldSuccess()
        {
            return AsynchronousTesting.WithCancellationTokenAsync(async cancellationToken =>
            {
                // Arrange.
                var address = new ReceivingAddress(
                    Guid.NewGuid(),
                    TestAddress.Regtest1,
                    false,
                    new Collection<ReceivingAddressReservation>());

                var addresses = new[] { address };

                this.repository
                    .Setup(r => r.ListAsync(AddressFilter.Available, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(addresses);

                this.choser
                    .Setup(c => c.Choose(addresses))
                    .Returns(address);

                this.repository
                    .Setup(r => r.TryLockAsync(address.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(() =>
                    {
                        var reservation = new ReceivingAddressReservation(Guid.NewGuid(), address, DateTime.Now, null);

                        address.Reservations.Add(reservation);

                        return reservation;
                    });

                // Act.
                var recv = await this.subject.TryLockAddressAsync(cancellationToken);

                // Assert.
                Assert.NotNull(recv);
                Assert.Equal(address, recv.Address);
                Assert.NotEqual(Guid.Empty, recv.Id);
                Assert.Null(recv.ReleasedDate);
                Assert.Equal(DateTime.Now, recv.ReservedDate, TimeSpan.FromSeconds(1));

                this.repository.Verify(
                    r => r.ListAsync(It.Is<AddressFilter>(f => f.HasFlag(AddressFilter.Available)), cancellationToken),
                    Times.Once());

                this.choser.Verify(c => c.Choose(addresses), Times.Once());
                this.repository.Verify(r => r.TryLockAsync(address.Id, cancellationToken), Times.Once());
            });
        }

        [Fact]
        public Task ReleaseAddressAsync_ReleaseFunctionInStorageShouldBeCalled()
        {
            return AsynchronousTesting.WithCancellationTokenAsync(async cancellationToken =>
            {
                // Arrange.
                var id = Guid.NewGuid();

                // Act.
                await this.subject.ReleaseAddressAsync(id, cancellationToken);

                // Assert.
                this.repository.Verify(r => r.ReleaseAsync(id, cancellationToken), Times.Once());
            });
        }
    }
}
