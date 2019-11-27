using System;

namespace Ztm.WebApi.AddressPools
{
    [Flags]
    public enum AddressFilter
    {
        NoFilter = 0,
        NeverUsed = 1,
        Available = 2,
    }
}