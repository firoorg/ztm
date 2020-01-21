using NBitcoin;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.WebApi.Models
{
    public class TransferRequest
    {
        public PropertyAmount Amount { get; set; }
        public BitcoinAddress Destination { get; set; }
        public Money ReferenceAmount { get; set; }
    }
}