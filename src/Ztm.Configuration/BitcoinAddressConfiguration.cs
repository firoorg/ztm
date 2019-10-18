using System.ComponentModel;
using NBitcoin;

namespace Ztm.Configuration
{
    [TypeConverter(typeof(BitcoinAddressConfigurationConverter))]
    public class BitcoinAddressConfiguration
    {
        public NetworkType Type { get; set; }
        public BitcoinAddress Address { get; set; }
    }
}
