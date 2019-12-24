using NBitcoin;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.WebApi.Models
{
    public sealed class Issuing
    {
        public PropertyAmount Amount { get; set; }
        public BitcoinAddress Destination { get; set; }
        public string Note { get; set; }
    }
}