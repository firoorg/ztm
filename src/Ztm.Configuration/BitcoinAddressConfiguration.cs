using System;
using System.ComponentModel;
using System.Text.RegularExpressions;
using NBitcoin;
using Ztm.Zcoin.NBitcoin;

namespace Ztm.Configuration
{
    [TypeConverter(typeof(BitcoinAddressConfigurationConverter))]
    public class BitcoinAddressConfiguration
    {
        public NetworkType Type { get; set; }
        public BitcoinAddress Address { get; set; }
    }
}