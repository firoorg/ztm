using System.Collections.Generic;
using System.Linq;

namespace Ztm.WebApi.AddressPools
{
    public sealed class LessUsageFirstChoser : IAddressChoser
    {
        public ReceivingAddress Choose(IEnumerable<ReceivingAddress> receivingAddress)
        {
            var chosen = receivingAddress.First();
            var currentUsage = chosen.ReceivingAddressReservations == null
                ? 0
                : chosen.ReceivingAddressReservations.Count;

            foreach (var candidate in receivingAddress)
            {
                var usage = candidate.ReceivingAddressReservations == null
                    ? 0
                    : candidate.ReceivingAddressReservations.Count;

                if (usage < currentUsage)
                {
                    chosen = candidate;
                    currentUsage = usage;
                }
            }

            return chosen;
        }
    }
}