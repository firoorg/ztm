using System;
using System.Collections.Generic;
using System.Linq;
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
            this.choser = Substitute.For<IAddressChoser>();

            this.subject = new ReceivingAddressPool(this.generator, this.storage, this.choser);

            MockChoser();
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
            this.generator.GenerateAsync(Arg.Any<CancellationToken>()).Returns(address);

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
            await this.storage.TryLockAsync(receivingAddress.Id, CancellationToken.None);

            // Act.
            var reservation = await this.subject.TryLockAddressAsync(CancellationToken.None);

            // Assert.
            Assert.Null(reservation);
        }

        [Fact]
        public async Task TryLockAddressAsync_AndHaveAnAvailable_ShouldReturnNull()
        {
            // Arrange.
            var address = TestAddress.Regtest1;
            await this.storage.AddAddressAsync(address, CancellationToken.None);

            // Act.
            var recv = await this.subject.TryLockAddressAsync(CancellationToken.None);

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

        void MockChoser()
        {
            this.choser.Choose(Arg.Any<IEnumerable<ReceivingAddress>>())
                .Returns(info => {
                    var addresses = info.ArgAt<IEnumerable<ReceivingAddress>>(0);

                    if (addresses == null || addresses.Count() == 0)
                    {
                        return null;
                    }

                    return addresses.First();
                });
        }
    }
}