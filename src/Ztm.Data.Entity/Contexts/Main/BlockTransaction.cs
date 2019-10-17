using System;
using NBitcoin;

namespace Ztm.Data.Entity.Contexts.Main
{
    public sealed class BlockTransaction : IComparable<BlockTransaction>
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

        public override bool Equals(object other)
        {
            if (other == null || GetType() != other.GetType())
            {
                return false;
            }

            return CompareTo((BlockTransaction)other) == 0;
        }

        public override int GetHashCode()
        {
            int hash = 0;

            hash ^= (BlockHash != null) ? BlockHash.GetHashCode() : 0;
            hash ^= (TransactionHash != null) ? TransactionHash.GetHashCode() : 0;
            hash ^= Index;

            return hash;
        }
    }
}
