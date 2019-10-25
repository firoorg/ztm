using NBitcoin;
using Xunit;

namespace Ztm.Zcoin.NBitcoin.Tests
{
    public class ZcoinTransactionTests
    {
        [Fact]
        public void IsCoinBase_WithCoinBaseTransaction_ShouldReturnTrue()
        {
            var tx = Transaction.Parse(ZcoinTransactionData.CoinBase, ZcoinNetworks.Instance.Mainnet);
            Assert.True(tx.IsCoinBase);
        }

        [Fact]
        public void IsCoinBase_WithNormalTransaction_ShouldReturnFalse()
        {
            var tx = Transaction.Parse(ZcoinTransactionData.Normal, ZcoinNetworks.Instance.Mainnet);
            Assert.False(tx.IsCoinBase);
        }

        [Fact]
        public void IsCoinBase_WithZecoinSpendTransaction_ShouldReturnFalse()
        {
            var tx = Transaction.Parse(ZcoinTransactionData.ZerocoinSpend, ZcoinNetworks.Instance.Mainnet);
            Assert.False(tx.IsCoinBase);
        }

        [Fact]
        public void IsCoinBase_WithSigmaSpendTransaction_ShouldReturnFalse()
        {
            var tx = Transaction.Parse(ZcoinTransactionData.SigmaSpend, ZcoinNetworks.Instance.Mainnet);
            Assert.False(tx.IsCoinBase);
        }

        [Fact]
        public void IsCoinBase_WithRemintTransaction_ShouldReturnFalse()
        {
            var tx = Transaction.Parse(ZcoinTransactionData.ZerocoinRemint, ZcoinNetworks.Instance.Regtest);
            Assert.False(tx.IsCoinBase);
        }

        [Fact]
        public void IsZerocoinSpend_WithZerocoinSpend_ShouldReturnTrue()
        {
            var tx = Transaction.Parse(ZcoinTransactionData.ZerocoinSpend, ZcoinNetworks.Instance.Mainnet);
            Assert.True(tx.IsZerocoinSpend());
        }

        [Theory]
        [InlineData(ZcoinTransactionData.CoinBase, NetworkType.Mainnet)]
        [InlineData(ZcoinTransactionData.Normal, NetworkType.Mainnet)]
        [InlineData(ZcoinTransactionData.SigmaSpend, NetworkType.Mainnet)]
        [InlineData(ZcoinTransactionData.ZerocoinRemint, NetworkType.Mainnet)]

        public void IsZerocoinSpend_WithNonZerocoinSpend_ShouldReturnFalse(string rawTransaction, NetworkType networkType)
        {
            var network = ZcoinNetworks.Instance.GetNetwork(networkType);
            var tx = Transaction.Parse(rawTransaction, network);

            Assert.False(tx.IsZerocoinSpend());
        }

        [Fact]
        public void IsSigmaSpend_WithSigmaSpend_ShouldReturnTrue()
        {
            var tx = Transaction.Parse(ZcoinTransactionData.SigmaSpend, ZcoinNetworks.Instance.Mainnet);
            Assert.True(tx.IsSigmaSpend());
        }

        [Theory]
        [InlineData(ZcoinTransactionData.CoinBase, NetworkType.Mainnet)]
        [InlineData(ZcoinTransactionData.Normal, NetworkType.Mainnet)]
        [InlineData(ZcoinTransactionData.ZerocoinSpend, NetworkType.Mainnet)]
        [InlineData(ZcoinTransactionData.ZerocoinRemint, NetworkType.Regtest)]

        public void IsSigmaSpend_WithNonSigmaSpend_ShouldReturnFalse(string rawTransaction, NetworkType networkType)
        {
            var network = ZcoinNetworks.Instance.GetNetwork(networkType);
            var tx = Transaction.Parse(rawTransaction, network);

            Assert.False(tx.IsSigmaSpend());
        }

        [Fact]
        public void IsZerocoinRemint_WithZerocoinRemint_ShouldReturnTrue()
        {
            var tx = Transaction.Parse(ZcoinTransactionData.ZerocoinRemint, ZcoinNetworks.Instance.Mainnet);
            Assert.True(tx.IsZerocoinRemint());
        }

        [Theory]
        [InlineData(ZcoinTransactionData.CoinBase, NetworkType.Mainnet)]
        [InlineData(ZcoinTransactionData.Normal, NetworkType.Mainnet)]
        [InlineData(ZcoinTransactionData.ZerocoinSpend, NetworkType.Mainnet)]
        [InlineData(ZcoinTransactionData.SigmaSpend, NetworkType.Mainnet)]

        public void IsZerocoinRemint_WithNonZerocoinRemint_ShouldReturnFalse(string rawTransaction, NetworkType networkType)
        {
            var network = ZcoinNetworks.Instance.GetNetwork(networkType);
            var tx = Transaction.Parse(rawTransaction, network);

            Assert.False(tx.IsZerocoinRemint());
        }
    }
}