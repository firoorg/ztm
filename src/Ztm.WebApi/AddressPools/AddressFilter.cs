using System;

namespace Ztm.WebApi.AddressPools
{
    [Flags]
    public enum AddressFilter
    {
        None = 0x00,
        NeverUsed = 0x01,
        Available = 0x02,
    }
}