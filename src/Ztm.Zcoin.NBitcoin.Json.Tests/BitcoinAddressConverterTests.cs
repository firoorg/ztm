using System;
using NBitcoin;
using Newtonsoft.Json;
using Xunit;

namespace Ztm.Zcoin.NBitcoin.Json.Tests
{
    public sealed class BitcoinAddressConverterTests
    {
        [Fact]
        public void Construct_WithNullNetwork_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "network",
                () => new BitcoinAddressConverter(null)
            );
        }

        [Theory]
        [InlineData("a8ULhhDgfdSiXJhSZVdhb8EuDc6R3ogsaM", NetworkType.Mainnet)]
        [InlineData("aBuZzbgtaqTsfF8eFfuHNaXWpQ7JbeQokz", NetworkType.Mainnet)]
        [InlineData("THXZEthKxPkHxT2xcp1pFbBcxZQtoBUs35", NetworkType.Testnet)]
        [InlineData("TDk19wPKYq91i18qmY6U9FeTdTxwPeSveo", NetworkType.Testnet)]
        [InlineData("THXZEthKxPkHxT2xcp1pFbBcxZQtoBUs35", NetworkType.Regtest)]
        [InlineData("TDk19wPKYq91i18qmY6U9FeTdTxwPeSveo", NetworkType.Regtest)]
        public void DeserializeObject_WithValidAddressAndMatchedNetwork_ShouldSuccess(string rawAddress, NetworkType networkType)
        {
            // Arrange.
            var json = JsonConvert.SerializeObject(rawAddress);
            var network = ZcoinNetworks.Instance.GetNetwork(networkType);
            var converter = new BitcoinAddressConverter(network);

            // Act.
            var result = JsonConvert.DeserializeObject<BitcoinAddress>(json, converter);

            // Assert.
            Assert.Equal(
                BitcoinAddress.Create(rawAddress, network),
                result
            );
        }

        [Theory]
        [InlineData("a8ULhhDgfdSiXJhSZVdhb8EuDc6R3ogsaM", NetworkType.Testnet)]
        [InlineData("aBuZzbgtaqTsfF8eFfuHNaXWpQ7JbeQokz", NetworkType.Regtest)]
        [InlineData("THXZEthKxPkHxT2xcp1pFbBcxZQtoBUs35", NetworkType.Mainnet)]
        [InlineData("TDk19wPKYq91i18qmY6U9FeTdTxwPeSveo", NetworkType.Mainnet)]
        public void DeserializeObject_WithUnmathedNetwork_ShouldThrow(string rawAddress, NetworkType networkType)
        {
            var json = JsonConvert.SerializeObject(rawAddress);
            var network = ZcoinNetworks.Instance.GetNetwork(networkType);
            var converter = new BitcoinAddressConverter(network);

            Assert.Throws<FormatException>(
                () => JsonConvert.DeserializeObject<BitcoinAddress>(json, converter)
            );
        }

        [Theory]
        [InlineData("a8ULhhDgfdSiXJhSZVdhb8EuDc6R3ogsa", NetworkType.Mainnet)]
        [InlineData("aBuZzbgtaqTsfF8eFfuHNaXWpQ7JbeQok", NetworkType.Mainnet)]
        [InlineData("THXZEthKxPkHxT2xcp1pFbBcxZQtoBUs3", NetworkType.Testnet)]
        [InlineData("TDk19wPKYq91i18qmY6U9FeTdTxwPeSve", NetworkType.Testnet)]
        [InlineData("THXZEthKxPkHxT2xcp1pFbBcxZQtoBUs3", NetworkType.Regtest)]
        [InlineData("TDk19wPKYq91i18qmY6U9FeTdTxwPeSve", NetworkType.Regtest)]
        public void DeserializeObject_WithInvalidAddress_ShouldThrow(string rawAddress, NetworkType networkType)
        {
            var json = JsonConvert.SerializeObject(rawAddress);
            var network = ZcoinNetworks.Instance.GetNetwork(networkType);
            var converter = new BitcoinAddressConverter(network);

            Assert.Throws<FormatException>(
                () => JsonConvert.DeserializeObject<BitcoinAddress>(json, converter)
            );
        }

        [Theory]
        [InlineData("a8ULhhDgfdSiXJhSZVdhb8EuDc6R3ogsaM", NetworkType.Mainnet)]
        [InlineData("aBuZzbgtaqTsfF8eFfuHNaXWpQ7JbeQokz", NetworkType.Mainnet)]
        [InlineData("THXZEthKxPkHxT2xcp1pFbBcxZQtoBUs35", NetworkType.Testnet)]
        [InlineData("TDk19wPKYq91i18qmY6U9FeTdTxwPeSveo", NetworkType.Testnet)]
        [InlineData("THXZEthKxPkHxT2xcp1pFbBcxZQtoBUs35", NetworkType.Regtest)]
        [InlineData("TDk19wPKYq91i18qmY6U9FeTdTxwPeSveo", NetworkType.Regtest)]
        public void SerializeObject_WithValidAddress_ShouldSuccess(string rawAddress, NetworkType networkType)
        {
            // Arrange.
            var network = ZcoinNetworks.Instance.GetNetwork(networkType);
            var address = BitcoinAddress.Create(rawAddress, network);

            var converter = new BitcoinAddressConverter(network);

            // Act.
            var json = JsonConvert.SerializeObject(address, Formatting.None, converter);

            // Assert.
            var serialized = JsonConvert.DeserializeObject<string>(json);
            Assert.Equal(rawAddress, serialized);
        }
    }
}