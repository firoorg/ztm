using NBitcoin;

namespace Ztm.WebApi.Models
{
    public sealed class Transaction
    {
        public uint256 Tx { get; set; }
        public Money Fee { get; set; }
    }
}