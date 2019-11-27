using System.Collections;
using System.Collections.Generic;

namespace Ztm.WebApi.AddressPools
{
    public interface IAddressChoser
    {
        /// <remarks>
        /// Garantee all receiving addresses that are send to this function will be available and the set is not empty.
        /// </remarks>
        ReceivingAddress Choose(IEnumerable<ReceivingAddress> addresses);
    }
}