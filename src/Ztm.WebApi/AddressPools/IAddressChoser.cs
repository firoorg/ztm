using System.Collections.Generic;

namespace Ztm.WebApi.AddressPools
{
    public interface IAddressChoser
    {
        /// <summary>
        /// Choose an address from addresses set.
        /// The set could not be null or empty.
        /// </summary>
        ReceivingAddress Choose(IEnumerable<ReceivingAddress> addresses);
    }
}