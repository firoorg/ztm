using System;
using System.ComponentModel;
using System.Text.RegularExpressions;
using NBitcoin;
using Ztm.Zcoin.NBitcoin;

namespace Ztm.Configuration
{
    public sealed class BitcoinAddressConfigurationConveter : TypeConverter
    {
        NetworkType GetNetworkType(string networkName)
        {
            NetworkType networkType;
            if (Enum.TryParse<NetworkType>(networkName, out networkType))
            {
                return networkType;
            }

            throw new ArgumentException($"Value is not valid.", nameof(networkType));
        }

        (string, string) ParseAddress(string address)
        {
            Regex rx = new Regex(@"^(\w+):(\w+)$", RegexOptions.Compiled);
            MatchCollection matcheds = rx.Matches(address);

            if (matcheds.Count < 1)
            {
                throw new FormatException($"Format is not valid.");
            }

            return (matcheds[0].Groups[1].Value, matcheds[0].Groups[2].Value);
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
        {
            if (!(value is string))
            {
                throw new NotSupportedException($"Cannot convert {value} to {typeof(BitcoinAddressConfiguration)}.");
            }

            var (networkName, address) = ParseAddress((string)value);

            var networkType = GetNetworkType(networkName);
            var network = ZcoinNetworks.Instance.GetNetwork(networkType);

            var configuration = new BitcoinAddressConfiguration();
            configuration.address = BitcoinAddress.Create(address, network);
            configuration.type = networkType;

            return configuration;
        }

        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType != typeof(string))
            {
                throw new NotSupportedException($"Cannot convert {typeof(BitcoinAddressConfiguration)} to {destinationType}");
            }

            var configuration = (BitcoinAddressConfiguration)value;
            return String.Format("{0}:{1}", configuration.type, configuration.address);
        }
    }
}