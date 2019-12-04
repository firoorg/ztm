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
            Assert.Throws<ArgumentException>(
                "addresses",
                () => this.subject.Choose(new Collection<ReceivingAddress>()));
        }

        [Theory]
        [InlineData(0, 0, 1, 1)]
        [InlineData(2, 2, 1, 1)]
        [InlineData(999, 1000, 999, 1000)]
        public void Choose_WithNonEmptyList_ShouldReturnLessUsageAddress(params int[] usages)
        {
            // Arrange.
            var availables = new List<ReceivingAddress>();
            var emptyReservations = new Collection<ReceivingAddressReservation>();
            var expected = usages.Min();

            foreach (var u in usages)
            {
                availables.Add(
                    new ReceivingAddress
                    (
                        Guid.NewGuid(),
                        TestAddress.Regtest1,
                        false,
                        new Collection<ReceivingAddressReservation>()
                    ));

                for (int i = 0; i < u; i++)
                {
                    var a = availables.Last();
                    a.Reservations.Add(new ReceivingAddressReservation(Guid.NewGuid(), a, DateTime.UtcNow, null));
                }
            }

            // Act.
            var chosen = this.subject.Choose(availables);

            // Assert.
            var usage = chosen.Reservations.Count;

            Assert.Equal(expected, usage);
        }
    }
}