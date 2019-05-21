using System;
using System.Collections.ObjectModel;
using NBitcoin;

namespace Ztm.Data.Entity.Contexts.Main
{
    public class Output : IComparable<Output>
    {
        public uint256 TransactionHash { get; set; }
        public long Index { get; set; }
        public long Value { get; set; }
        public Script Script { get; set; }

        public Transaction Transaction { get; set; }

        public Collection<Input> Inputs { get; set; }

        public int CompareTo(Output other)
        {
            if (other == null)
            {
                return 1;
            }

            // Check transaction hash.
            if (TransactionHash < other.TransactionHash)
            {
                return -1;
            }
            else if (TransactionHash > other.TransactionHash)
            {
                return 1;
            }

            // Check index.
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
