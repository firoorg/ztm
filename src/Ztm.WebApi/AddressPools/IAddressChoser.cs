using System.Collections;
using System.Collections.Generic;

namespace Ztm.WebApi.AddressPools
{
    public interface IAddressChoser
    {
        // Garantee all receiving addresses that are send to this function will be available and size of set is not be null.
        ReceivingAddress Choose(IEnumerable<ReceivingAddress> receivingAddress);
    }
}