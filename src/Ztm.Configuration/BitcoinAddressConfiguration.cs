using System;
using System.ComponentModel;
using System.Text.RegularExpressions;
using NBitcoin;
using Ztm.Zcoin.NBitcoin;

namespace Ztm.Configuration
{
    [TypeConverter(typeof(BitcoinAddressConfigurationConveter))]
    public class BitcoinAddressConfiguration
    {
        public NetworkType type { get; set; }
        public BitcoinAddress address { get; set; }
    }
}