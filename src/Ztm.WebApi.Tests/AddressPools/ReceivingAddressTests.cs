using System;
using System.Collections.ObjectModel;
using Xunit;
using Ztm.Testing;
using Ztm.WebApi.AddressPools;

namespace Ztm.WebApi.Tests.AddressPools
{
    public sealed class ReceivingAddressTests
    {
        [Fact]
        public void Construct_WithNullReservations_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "reservations",
                () => new ReceivingAddress(Guid.NewGuid(), TestAddress.Regtest1, false, null));
        }

        [Fact]
        public void Construct_WithNullAddress_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "address",
                () => new ReceivingAddress(Guid.NewGuid(), null, false, new Collection<ReceivingAddressReservation>()));
        }

        [Fact]
        public void Construct_WithValidArgs_ShouldSuccess()
        {
            // Arrange.
            var id = Guid.NewGuid();
            var address = TestAddress.Mainnet1;
            var isLocked = false;
            var reservations = new Collection<ReceivingAddressReservation>();

            // Act.
            var r = new ReceivingAddress(id, address, isLocked, reservations);

            // Assert.
            Assert.Equal(id, r.Id);
            Assert.Equal(address, r.Address);
            Assert.Equal(isLocked, r.IsLocked);
            Assert.Same(reservations, r.Reservations);
        }

        [Fact]
        public void Equals_WithSameId_ShouldReturnTrue()
        {
            var id = Guid.NewGuid();
            var address = TestAddress.Mainnet1;
            var isLocked = false;
            var reservations = new Collection<ReceivingAddressReservation>();
            var r = new ReceivingAddress(id, address, isLocked, reservations);

            ReceivingAddress another;

            another = new ReceivingAddress(id, TestAddress.Regtest1, isLocked, reservations);
            Assert.True(r.Equals(another));

            another = new ReceivingAddress(id, address, true, reservations);
            Assert.True(r.Equals(another));

            another = new ReceivingAddress(id, address, isLocked, new Collection<ReceivingAddressReservation>());
            Assert.True(r.Equals(another));
        }

        [Fact]
        public void Equals_WithDifferentId_ShouldReturnFalse()
        {
            var id = Guid.NewGuid();
            var address = TestAddress.Mainnet1;
            var isLocked = false;
            var reservations = new Collection<ReceivingAddressReservation>();
            var r = new ReceivingAddress(id, address, isLocked, reservations);

            ReceivingAddress another = new ReceivingAddress(Guid.NewGuid(), address, isLocked, reservations);

            Assert.False(r.Equals(another));
        }
    }
}