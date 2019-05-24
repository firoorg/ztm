using System;
using System.Collections.Generic;
using NBitcoin;

namespace Ztm.Data.Entity.Contexts.Main
{
    public class Block : IComparable<Block>
    {
        public Block()
        {
            Transactions = new SortedSet<BlockTransaction>();
        }

        public int Height { get; set; }
        public uint256 Hash { get; set; }
        public int Version { get; set; }
        public Target Bits { get; set; }
        public long Nonce { get; set; }
        public DateTime Time { get; set; }
        public uint256 MerkleRoot { get; set; }

        public SortedSet<BlockTransaction> Transactions { get; set; }

        public int CompareTo(Block other)
        {
            if (other == null)
            {
                return 1;
            }

            return Height - other.Height;
        }
    }
}
