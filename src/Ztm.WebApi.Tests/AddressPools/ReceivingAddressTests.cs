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
    }
}