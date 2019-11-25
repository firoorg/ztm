using System.Collections.Generic;
using System.Linq;
using Ztm.WebApi.AddressPools;

namespace Ztm.WebApi.Tests.AddressPools
{
    public class TestChoser : IAddressChoser
    {
        public virtual ReceivingAddress Choose(IEnumerable<ReceivingAddress> receivingAddress)
        {
            return receivingAddress.First();
        }
    }
}