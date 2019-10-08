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
            int result;

            if (other == null)
            {
                return 1;
            }

            if ((result = BlockHash.CompareTo(other.BlockHash)) != 0)
            {
                return result;
            }

            return Index - other.Index;
        }

        public bool Equals(BlockTransaction other)
        {
            return CompareTo(other) == 0;
        }
    }
}
