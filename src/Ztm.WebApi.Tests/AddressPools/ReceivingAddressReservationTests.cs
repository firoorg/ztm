using System;
using System.Collections.ObjectModel;
using Xunit;
using Ztm.Testing;
using Ztm.WebApi.AddressPools;

namespace Ztm.WebApi.Tests.AddressPools
{
    public sealed class ReceivingAddressReservationTests
    {
        [Fact]
        public void Construct_WithNullAddress_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "address",
                () => new ReceivingAddressReservation(Guid.NewGuid(), null, DateTime.UtcNow, null)
            );
        }

        [Fact]
        public void Construct_WithValidArgs_ShouldBeSet()
        {
            // Arrange.
            var id = Guid.NewGuid();
            var address = new ReceivingAddress(
                Guid.NewGuid(),
                TestAddress.Mainnet1,
                false,
                new Collection<ReceivingAddressReservation>());
            var reserved = DateTime.UtcNow;

            // Act.
            var r = new ReceivingAddressReservation
            (
                id,
                address,
                reserved,
                null
            );

            // Assert.
            Assert.Equal(id, r.Id);
            Assert.Equal(address, r.Address);
            Assert.Equal(reserved, r.ReservedDate);
            Assert.Null(r.ReleasedDate);
        }

        [Fact]
        public void Construct_WithValidArgsAndNotNullReleasedTime_ShouldBeSet()
        {
            // Arrange.
            var id = Guid.NewGuid();
            var address = new ReceivingAddress(
                Guid.NewGuid(),
                TestAddress.Mainnet1,
                false,
                new Collection<ReceivingAddressReservation>());
            var reserved = DateTime.UtcNow;
            var released = reserved.Add(TimeSpan.FromHours(10));

            // Act.
            var r = new ReceivingAddressReservation
            (
                id,
                address,
                reserved,
                released
            );

            // Assert.
            Assert.Equal(id, r.Id);
            Assert.Equal(address, r.Address);
            Assert.Equal(reserved, r.ReservedDate);
            Assert.Equal(released, r.ReleasedDate);
        }
    }
}