using System.Collections.Generic;
using NBitcoin;

namespace Ztm.Data.Entity.Contexts.Main
{
    public class Transaction
    {
        public uint256 Hash { get; set; }
        public uint256 BlockHash { get; set; }
        public long Version { get; set; }
        public long LockTime { get; set; }

        public Block Block { get; set; }

        public SortedSet<Input> Inputs { get; set; }
        public SortedSet<Output> Outputs { get; set; }
    }
}
