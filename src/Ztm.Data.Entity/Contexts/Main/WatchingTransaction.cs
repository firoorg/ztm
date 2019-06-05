using System;
using NBitcoin;

namespace Ztm.Data.Entity.Contexts.Main
{
    public class WatchingTransaction
    {
        public uint256 Hash { get; set; }
        public Guid Listener { get; set; }
        public DateTime StartTime { get; set; }
    }
}
