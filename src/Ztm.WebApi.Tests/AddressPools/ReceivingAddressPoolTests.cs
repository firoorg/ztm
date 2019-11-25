using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using NSubstitute;
using Xunit;
using Ztm.Testing;
using Ztm.WebApi.AddressPools;

namespace Ztm.WebApi.Tests.AddressPools
{
    public sealed class ReceivingAddressPoolTests
    {
        readonly IReceivingAddressStorage storage;
        readonly IAddressGenerator generator;
        readonly IAddressChoser choser;

        readonly ReceivingAddressPool subject;

        public ReceivingAddressPoolTests()
        {
            this.generator = Substitute.For<IAddressGenerator>();
            this.storage = Substitute.ForPartsOf<TestReceivingAddressStorage>();
            this.choser = new TestChoser();

            this.subject = new ReceivingAddressPool(this.generator, this.storage, this.choser);
        }

        [Fact]
        public void Construct_WithArgs_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "generator",
                () => new ReceivingAddressPool(null, this.storage, this.choser));

            Assert.Throws<ArgumentNullException>(
                "storage",
                () => new ReceivingAddressPool(this.generator, null, this.choser));

            Assert.Throws<ArgumentNullException>(
                "choser",
                () => new ReceivingAddressPool(this.generator, this.storage, null));
        }

        [Fact]
        public async Task GenerateAddressAsync_GenerateAndAddAddressAsyncShouldBeActivate()
        {
            // Arrange.
            var address = TestAddress.Regtest1;
            this.generator.GenerateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(address));

            // Act.
            await this.subject.GenerateAddressAsync(CancellationToken.None);

            // Assert.
            _ = this.generator.Received(1).GenerateAsync(Arg.Any<CancellationToken>());
            _ = this.storage.Received(1).AddAddressAsync(Arg.Is<BitcoinAddress>(address), Arg.Any<CancellationToken>());
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
            var address = TestAddress.Regtest1;
            var receivingAddress = await this.storage.AddAddressAsync(address, CancellationToken.None);
            await this.storage.SetLockedStatusAsync(receivingAddress.Id, true, CancellationToken.None);

            // Act.
            var reservation = await this.subject.TryLockAddressAsync(CancellationToken.None);

            // Assert.
            Assert.Null(reservation);
        }

        [Fact]
        public async Task TryLockAddressAsync_AndHaveAnAvailable_ShouldNotReturnNull()
        {
            // Arrange.
            var address = TestAddress.Regtest1;
            var recv = await this.storage.AddAddressAsync(address, CancellationToken.None);

            // Act.
            var resv = await this.subject.TryLockAddressAsync(CancellationToken.None);

            // Assert.
            Assert.NotNull(recv);
        }

        [Fact]
        public async Task TryLockAddressAsync_AndConsumedAllAvailable_ShouldReturnNullWhenTryToLockNext()
        {
            // Arrange.
            await this.storage.AddAddressAsync(TestAddress.Regtest1, CancellationToken.None);
            await this.storage.AddAddressAsync(TestAddress.Regtest2, CancellationToken.None);

            // Act.
            await this.subject.TryLockAddressAsync(CancellationToken.None);
            await this.subject.TryLockAddressAsync(CancellationToken.None);

            var recv = await this.subject.TryLockAddressAsync(CancellationToken.None);

            // Assert.
            Assert.Null(recv);
        }

        [Fact]
        public async Task TryLockAddressAsync_PoolHaveAReleaed_ShouldSuccess()
        {
            // Arrange.
            var address = TestAddress.Regtest1;
            var recv = await this.storage.AddAddressAsync(address, CancellationToken.None);
            var reservation1 = await this.subject.TryLockAddressAsync(CancellationToken.None);

            await this.subject.ReleaseAsync(reservation1.Id, CancellationToken.None);

            // Act.
            var reservation2 = await this.subject.TryLockAddressAsync(CancellationToken.None);

            // Assert.
            Assert.NotEqual(reservation1.Id, reservation2.Id);
            Assert.Null(reservation2.ReleasedDate);
        }

        [Fact]
        public async Task ReleaseAsync_UnreleasedReservation_ShouldSuccess()
        {
            // Arrange.
            var address = TestAddress.Regtest1;
            var recv = await this.storage.AddAddressAsync(address, CancellationToken.None);
            var resv = await this.subject.TryLockAddressAsync(CancellationToken.None);

            // Act.
            await this.subject.ReleaseAsync(resv.Id, CancellationToken.None);

            // Assert.
            var updatedRecv = await this.storage.GetAsync(recv.Id, CancellationToken.None);
            var updatedResv = await this.storage.GetReservationAsync(resv.Id, CancellationToken.None);

            Assert.False(updatedRecv.IsLocked);
            Assert.NotNull(updatedResv.ReleasedDate);
        }

        [Fact]
        public void ReleasedAsync_WithNotExistReservation_ShouldThrow()
        {
            _ = Assert.ThrowsAsync<KeyNotFoundException>(
                () => this.subject.ReleaseAsync(Guid.NewGuid(), CancellationToken.None));
        }

        [Fact]
        public async Task ReleasedAsync_Released_ShouldThrow()
        {
            var address = TestAddress.Regtest1;
            var recv = await this.storage.AddAddressAsync(address, CancellationToken.None);
            var resv = await this.subject.TryLockAddressAsync(CancellationToken.None);

            await this.subject.ReleaseAsync(resv.Id, CancellationToken.None);

            _ = Assert.ThrowsAsync<InvalidOperationException>(
                () => this.subject.ReleaseAsync(resv.Id, CancellationToken.None));
        }
    }
}