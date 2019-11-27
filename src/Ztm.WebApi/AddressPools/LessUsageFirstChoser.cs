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
                throw new ArgumentOutOfRangeException(nameof(addresses), "Addresses could not be empty.");
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