using System;
using NBitcoin;
using Xunit;
using Ztm.Zcoin.NBitcoin;

namespace Ztm.Configuration.Tests
{
    public class BitcoinAddressConfigurationConverterTests
    {
        readonly BitcoinAddressConfigurationConverter subject;

        public BitcoinAddressConfigurationConverterTests()
        {
            this.subject = new BitcoinAddressConfigurationConverter();
        }

        [Fact]
        public void CanConvertFrom_WithStringType_ShouldReturnTrue()
        {
            Assert.True(this.subject.CanConvertFrom(typeof(string)));
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(byte))]
        public void CanConvertFrom_WithOtherType_ShouldReturnFalse(Type type)
        {
            Assert.False(this.subject.CanConvertFrom(type));
        }

        [Fact]
        public void CanConvertTo_WithStringType_ShouldReturnTrue()
        {
            Assert.True(this.subject.CanConvertTo(typeof(string)));
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(long))]
        [InlineData(typeof(byte))]
        public void CanConvertTo_WithOtherType_ShouldReturnFalse(Type type)
        {
            Assert.False(this.subject.CanConvertTo(type));
        }

        [Theory]
        [InlineData("Mainnet:a8ULhhDgfdSiXJhSZVdhb8EuDc6R3ogsaM", NetworkType.Mainnet)]
        [InlineData("Testnet:TEDC38GBncNgtd2pVXeDhLeUGwJmXsiJBA", NetworkType.Testnet)]
        [InlineData("Regtest:TEDC38GBncNgtd2pVXeDhLeUGwJmXsiJBA", NetworkType.Regtest)]
        public void ConvertFrom_WithValidAddress_ShouldSuccess(string address, NetworkType expectedNetwork)
        {
            var configuration = (BitcoinAddressConfiguration)this.subject.ConvertFrom(address);

            Assert.Equal(expectedNetwork, configuration.Type);
            Assert.Equal(address.Split(':')[1], configuration.Address.ToString());
        }

        [Fact]
        public void ConvertFrom_WithInvalidNetworkPrefix_ShouldThrow()
        {
            Assert.Throws<NotSupportedException>(
                () => this.subject.ConvertFrom("inexistnetwork:a8ULhhDgfdSiXJhSZVdhb8EuDc6R3ogsaM")
            );
        }

        [Theory]
        [InlineData("")]
        [InlineData("a8ULhhDgfdSiXJhSZVdhb8EuDc6R3ogsaM")]
        [InlineData("Mainnet:a8ULhhDgfdSiXJhSZVdhb8EuDc6R3ogsaM:extended")]
        public void ConvertFrom_WithInvalidFormat_ShouldThrow(string address)
        {
            Assert.Throws<NotSupportedException>(
                () => this.subject.ConvertFrom(address)
            );
        }

        [Theory]
        [InlineData(0)]
        [InlineData(0L)]
        public void ConvertFrom_WithUnsupportedType_ShouldThrow(object value)
        {
            Assert.Throws<NotSupportedException>(
                () => this.subject.ConvertFrom(value)
            );
        }

        [Theory]
        [InlineData("Mainnet:TEDC38GBncNgtd2pVXeDhLeUGwJmXsiJBA")]
        [InlineData("Testnet:a8ULhhDgfdSiXJhSZVdhb8EuDc6R3ogsaM")]
        [InlineData("Regtest:a8ULhhDgfdSiXJhSZVdhb8EuDc6R3ogsaM")]
        public void ConvertFrom_WithMissmatchNetwork_ShouldThrow(string address)
        {
            Assert.Throws<NotSupportedException>(
                () => this.subject.ConvertFrom(address)
            );
        }

        [Theory]
        [InlineData("a8ULhhDgfdSiXJhSZVdhb8EuDc6R3ogsaM", NetworkType.Mainnet, "Mainnet:a8ULhhDgfdSiXJhSZVdhb8EuDc6R3ogsaM")]
        [InlineData("TEDC38GBncNgtd2pVXeDhLeUGwJmXsiJBA", NetworkType.Testnet, "Testnet:TEDC38GBncNgtd2pVXeDhLeUGwJmXsiJBA")]
        [InlineData("TEDC38GBncNgtd2pVXeDhLeUGwJmXsiJBA", NetworkType.Regtest, "Regtest:TEDC38GBncNgtd2pVXeDhLeUGwJmXsiJBA")]
        public void ConvertTo_WithValidValue_ShouldSuccess(string address, NetworkType networkType, string expectedOutput)
        {
            var configuration = new BitcoinAddressConfiguration();
            configuration.Type = networkType;
            configuration.Address = BitcoinAddress.Create(address, ZcoinNetworks.Instance.GetNetwork(networkType));

            // Assert.
            Assert.Equal(expectedOutput, this.subject.ConvertTo(configuration, typeof(string)));
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(long))]
        public void ConvertTo_WithUnsupportType_ShouldThrow(Type type)
        {
            var configuration = new BitcoinAddressConfiguration();
            configuration.Type = NetworkType.Mainnet;
            configuration.Address = BitcoinAddress.Create(
                "a8ULhhDgfdSiXJhSZVdhb8EuDc6R3ogsaM",
                ZcoinNetworks.Instance.GetNetwork(NetworkType.Mainnet)
            );

            Assert.Throws<NotSupportedException>(
                () => this.subject.ConvertTo(configuration, type)
            );
        }
    }
}