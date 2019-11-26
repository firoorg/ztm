using System;
using Xunit;
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
    }
}