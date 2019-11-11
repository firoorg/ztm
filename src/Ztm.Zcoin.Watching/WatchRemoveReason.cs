using System;

namespace Ztm.Zcoin.Watching
{
    [Flags]
    public enum WatchRemoveReason
    {
        None            = 0x00000000,
        Completed       = 0x00000001,
        BlockRemoved    = 0x00000002
    }
}
