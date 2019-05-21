using System;
using NBitcoin;

namespace Ztm.Data.Entity.Contexts.Main
{
    public class Input : IComparable<Input>
    {
        public uint256 TransactionHash { get; set; }
        public long Index { get; set; }
        public uint256 OutputHash { get; set; }
        public long OutputIndex { get; set; }
        public Script Script { get; set; }
        public long Sequence { get; set; }

        public Output Output { get; set; }
        public Transaction Transaction { get; set; }

        public int CompareTo(Input other)
        {
            if (other == null)
            {
                return 1;
            }

            if (TransactionHash < other.TransactionHash)
            {
                return -1;
            }
            else if (TransactionHash > other.TransactionHash)
            {
                return 1;
            }

            if (Index < other.Index)
            {
                return -1;
            }
            else if (Index > other.Index)
            {
                return 1;
            }

            return 0;
        }
    }
}
