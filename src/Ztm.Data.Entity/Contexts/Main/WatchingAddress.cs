using System;

namespace Ztm.Data.Entity.Contexts.Main
{
    public class WatchingAddress
    {
        public string Address { get; set; }
        public AddressWatchingType Type { get; set; }
        public Guid Listener { get; set; }
        public DateTime StartTime { get; set; }
    }

    public enum AddressWatchingType : byte
    {
        Credit,
        Debit
    }
}
