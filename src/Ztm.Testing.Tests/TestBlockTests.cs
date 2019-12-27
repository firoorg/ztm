using NBitcoin;
using Xunit;
using Ztm.Zcoin.NBitcoin;

namespace Ztm.Testing.Tests
{
    public sealed class TestBlockTests
    {
        [Fact]
        public void Mainnet117563_WhenRead_ShouldReturnCorrectValue()
        {
            var block = TestBlock.Mainnet117563;

            Assert.Equal(uint256.Parse("3c69e19de9889590adbb365cfa20acab64ab67eccdfea29aed735275dd027734"), block.GetHash());
            Assert.False(block.Header.IsMtp());
        }

        [Fact]
        public void Mainnet117564_WhenRead_ShouldReturnCorrectValue()
        {
            var block = TestBlock.Mainnet117564;

            Assert.Equal(uint256.Parse("f3646083759fd81d6cddeb6667deddde6745c77346ac81d58f282162763bc8aa"), block.GetHash());
            Assert.True(block.Header.IsMtp());
        }
    }
}
