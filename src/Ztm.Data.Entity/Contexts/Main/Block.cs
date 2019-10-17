using System;
using System.Collections.Generic;
using NBitcoin;

namespace Ztm.Data.Entity.Contexts.Main
{
    public sealed class Block : IComparable<Block>
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
        public int? MtpVersion { get; set; }
        public uint256 MtpHashValue { get; set; }
        public uint256 Reserved1 { get; set; }
        public uint256 Reserved2 { get; set; }

        public SortedSet<BlockTransaction> Transactions { get; set; }

        public int CompareTo(Block other)
        {
            if (other == null)
            {
                return 1;
            }

            if (Height != other.Height)
            {
                return Height - other.Height;
            }

            if (Hash != null)
            {
                return Hash.CompareTo(other.Hash);
            }
            else if (other.Hash != null)
            {
                return -1;
            }

            return 0;
        }

        public override bool Equals(object other)
        {
            if (other == null || GetType() != other.GetType())
            {
                return false;
            }

            return CompareTo((Block)other) == 0;
        }

        public override int GetHashCode()
        {
            return Height ^ (Hash != null ? Hash.GetHashCode() : 0);
        }
    }
}
