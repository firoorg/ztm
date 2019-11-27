using System;
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
    }
}