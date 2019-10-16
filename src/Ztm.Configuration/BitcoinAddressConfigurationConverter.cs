using System;
using System.ComponentModel;
using System.Text.RegularExpressions;
using NBitcoin;
using Ztm.Zcoin.NBitcoin;

namespace Ztm.Configuration
{
    public sealed class BitcoinAddressConfigurationConverter : TypeConverter
    {
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
                throw new NotSupportedException($"Cannot convert {value.GetType()} to {typeof(BitcoinAddressConfiguration)}.");
            }

            string networkName, address;

            try
            {
                (networkName, address) = ParseAddress((string)value);
            }
            catch (FormatException ex)
            {
                throw new NotSupportedException($"Cannot convert {(string)value} to {typeof(BitcoinAddressConfiguration)}.", ex);
            }

            NetworkType networkType;

            try
            {
                networkType = GetNetworkType(networkName);
            }
            catch (ArgumentException ex)
            {
                throw new NotSupportedException($"Network {networkName} is not supported.", ex);
            }

            var network = ZcoinNetworks.Instance.GetNetwork(networkType);

            BitcoinAddressConfiguration configuration;

            try
            {
                configuration = new BitcoinAddressConfiguration
                {
                    Address = BitcoinAddress.Create(address, network),
                    Type = networkType
                };
            }
            catch (FormatException ex)
            {
                throw new NotSupportedException($"Cannot convert {(string)value} to {typeof(BitcoinAddressConfiguration)}.", ex);
            }

            return configuration;
        }

        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType != typeof(string))
            {
                throw new NotSupportedException($"Cannot convert {typeof(BitcoinAddressConfiguration)} to {destinationType}.");
            }

            var configuration = (BitcoinAddressConfiguration)value;
            return $"{configuration.Type}:{configuration.Address}";
        }

        static NetworkType GetNetworkType(string networkName)
        {
            NetworkType networkType;
            if (Enum.TryParse<NetworkType>(networkName, out networkType))
            {
                return networkType;
            }

            throw new ArgumentException("Value is not valid.", nameof(networkType));
        }

        static readonly Regex regex = new Regex(@"^(\w+):(\w+)$", RegexOptions.Compiled);

        static (string networkName, string address) ParseAddress(string address)
        {
            MatchCollection matcheds = regex.Matches(address);

            if (matcheds.Count < 1)
            {
                throw new FormatException("Format is not valid.");
            }

            return (matcheds[0].Groups[1].Value, matcheds[0].Groups[2].Value);
        }
    }
}