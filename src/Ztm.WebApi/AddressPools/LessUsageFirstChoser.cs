using System;
using System.Collections.Generic;
using System.Linq;

namespace Ztm.WebApi.AddressPools
{
    public sealed class LessUsageFirstChoser : IAddressChoser
    {
        public ReceivingAddress Choose(IEnumerable<ReceivingAddress> addresses)
        {
            if (addresses == null)
            {
                throw new ArgumentNullException(nameof(addresses));
            }

            if (!addresses.Any())
            {
                throw new ArgumentException("Addresses could not be empty.", nameof(addresses));
            }

            return addresses.Aggregate
            (
                (previous, next) => next.Reservations.Count < previous.Reservations.Count
                    ? next
                    : previous
            );
        }
    }
}