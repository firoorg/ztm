using System;

namespace Ztm.Zcoin.Synchronization.Watchers.Rules
{
    [Flags]
    public enum BalanceChangeType
    {
        None    = 0x0,
        Credit  = 0x1,
        Debit   = 0x2
    }
}
