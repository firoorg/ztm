using System;

namespace Ztm.Zcoin.Synchronization
{
    [Flags]
    public enum AddressListeningType
    {
        None    = 0x0,
        Credit  = 0x1,
        Debit   = 0x2
    }
}
