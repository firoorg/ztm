using System;
using System.Collections.Generic;
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

        [Theory]
        [InlineData(0, 0, 0, 1, 1)]
        [InlineData(1, 2, 2, 1, 1)]
        [InlineData(999, 999, 1000, 999, 1000)]
        [InlineData(0, -1, -1, 1, 1)]
        [InlineData(0, 0, -1, -1)]
        public void Choose_FromList(int expected, params int[] vs)
        {
            // Arrange.
            var availables = new List<ReceivingAddress>();

            foreach (var v in vs)
            {
                var r = new ReceivingAddress
                (
                    Guid.NewGuid(),
                    TestAddress.Regtest1,
                    false,
                    null
                );

                var reservations = v < 0 ? null : new List<ReceivingAddressReservation>();
                if (reservations != null)
                {
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
                }

                availables.Add(
                    new ReceivingAddress
                    (
                        r.Id,
                        r.Address,
                        r.Available,
                        reservations
                    ));
            }

            // Act.
            var chosen = this.subject.Choose(availables);

            // Assert.
            var usage = chosen.ReceivingAddressReservations == null
                ? 0
                : chosen.ReceivingAddressReservations.Count;

            Assert.Equal(expected, usage);
        }
    }
}