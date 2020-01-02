using System;
using NBitcoin;

namespace Ztm.Zcoin.Rpc
{
    public class ExodusTransactionInformation
    {
        public uint256 TxId { get; set; }
        public BitcoinAddress SendingAddress { get; set; }
        public BitcoinAddress ReferenceAddress { get; set; }
        public bool IsMine { get; set; }
        public int Confirmations { get; set; }
        public Money Fee { get; set; }
        public int? Block { get; set; }
        public uint256 BlockHash { get; set; }
        public DateTimeOffset? BlockTime { get; set; }
        public bool? Valid { get; set; }
        public string InvalidReason { get; set; }

        public int Version { get; set; }
        public int TypeInt { get; set; }
        public string Type { get; set; }
    }
}
