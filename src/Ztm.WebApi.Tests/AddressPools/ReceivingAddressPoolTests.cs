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
            this.storage = Substitute.ForPartsOf<FakeReceivingAddressStorage>();
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
            var cancellationToken = new CancellationToken(true);

            // Act.
            await this.subject.GenerateAddressAsync(cancellationToken);

            // Assert.
            _ = this.generator.Received(1).GenerateAsync(Arg.Is<CancellationToken>(c => c == cancellationToken));
            _ = this.storage.Received(1).AddAsync(Arg.Is<BitcoinAddress>(address), Arg.Is<CancellationToken>(c => c == CancellationToken.None));
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
            var receivingAddress = await this.storage.AddAsync(address, CancellationToken.None);
            await this.storage.TryLockAsync(receivingAddress.Id, CancellationToken.None);

            // Act.
            var reservation = await this.subject.TryLockAddressAsync(CancellationToken.None);

            // Assert.
            Assert.Null(reservation);
        }

        [Fact]
        public async Task TryLockAddressAsync_AndHaveAnAvailable_ShouldSuccess()
        {
            // Arrange.
            var address = TestAddress.Regtest1;
            var r = await this.storage.AddAsync(address, CancellationToken.None);
            var cancellationToken = new CancellationToken(true);

            // Act.
            var recv = await this.subject.TryLockAddressAsync(cancellationToken);

            // Assert.
            Assert.NotNull(recv);
            _ = this.storage.Received(1).ListAsync(Arg.Any<AddressFilter>(), Arg.Is<CancellationToken>(c => c == cancellationToken));
            _ = this.choser.Received(1).Choose(Arg.Is<IEnumerable<ReceivingAddress>>(rs => rs.Any()));
            _ = this.storage.Received(1).TryLockAsync(Arg.Is<Guid>(id => id == r.Id), Arg.Is<CancellationToken>(c => c == cancellationToken));
        }

        [Fact]
        public async Task ReleaseAddressAsync_ReleaseFunctionInStorageShouldBeCalled()
        {
            // Arrange.
            var id = Guid.NewGuid();
            this.storage.ReleaseAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
            var cancellationToken = new CancellationToken(true);

            // Act.
            await this.subject.ReleaseAddressAsync(id, cancellationToken);

            // Assert.
            _ = this.storage.Received(1).ReleaseAsync(Arg.Is<Guid>(id), Arg.Is<CancellationToken>(c => c == cancellationToken));
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