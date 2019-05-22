using System;
using NBitcoin;

namespace Ztm.Data.Entity.Contexts.Main
{
    public class BlockTransaction : IComparable<BlockTransaction>
    {
        public uint256 BlockHash { get; set; }
        public uint256 TransactionHash { get; set; }
        public int Index { get; set; }

        public Block Block { get; set; }
        public Transaction Transaction { get; set; }

        public int CompareTo(BlockTransaction other)
        {
            if (other == null)
            {
                return 1;
            }

            switch (Block.CompareTo(other.Block))
            {
                case var result when result != 0:
                    return result;
            }

            return Index - other.Index;
        }
    }
}
