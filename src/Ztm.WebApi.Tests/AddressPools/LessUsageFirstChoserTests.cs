using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Xunit;
using Ztm.Testing;
using Ztm.WebApi.AddressPools;

namespace Ztm.WebApi.Tests.AddressPools
{
    public sealed class LessUsageFirstChoserTests
    {
        readonly LessUsageFirstChoser subject;

        public LessUsageFirstChoserTests()
        {
            this.subject = new LessUsageFirstChoser();
        }

        [Fact]
        public void Choose_WithNullAddresses_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "addresses",
                () => this.subject.Choose(null));
        }

        [Fact]
        public void Choose_WithEmptyAddresses_ShouldThrow()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                "addresses",
                () => this.subject.Choose(new Collection<ReceivingAddress>()));
        }

        [Theory]
        [InlineData(0, 0, 0, 1, 1)]
        [InlineData(1, 2, 2, 1, 1)]
        [InlineData(999, 999, 1000, 999, 1000)]
        public void Choose_FromList(int expected, params int[] vs)
        {
            // Arrange.
            var availables = new List<ReceivingAddress>();
            var emptyReservations = new Collection<ReceivingAddressReservation>();

            foreach (var v in vs)
            {
                var r = new ReceivingAddress
                (
                    Guid.NewGuid(),
                    TestAddress.Regtest1,
                    false,
                    emptyReservations
                );

                var reservations = new List<ReceivingAddressReservation>();

                foreach (var _ in Enumerable.Range(0, v))
                {
                    reservations.Add(
                        new ReceivingAddressReservation
                        (
                            Guid.NewGuid(),
                            r,
                            DateTime.UtcNow,
                            null
                        ));
                }

                availables.Add(

                    new ReceivingAddress
                    (
                        r.Id,
                        r.Address,
                        r.IsLocked,
                        reservations
                    ));
            }

            // Act.
            var chosen = this.subject.Choose(availables);

            // Assert.
            var usage = chosen.Reservations.Count;

            Assert.Equal(expected, usage);
        }
    }
}