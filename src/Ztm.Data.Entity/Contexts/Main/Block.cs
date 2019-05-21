using System;
using System.Collections.ObjectModel;
using NBitcoin;

namespace Ztm.Data.Entity.Contexts.Main
{
    public class Block
    {
        public int Height { get; set; }
        public uint256 Hash { get; set; }
        public int Version { get; set; }
        public Target Bits { get; set; }
        public long Nonce { get; set; }
        public DateTime Time { get; set; }
        public uint256 MerkleRoot { get; set; }

        public Collection<Transaction> Transactions { get; set; }
    }
}
