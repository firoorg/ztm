using NBitcoin;
using Xunit;

namespace Ztm.Zcoin.NBitcoin.Tests
{
    public class ZcoinBlockTests
    {
        [Fact]
        public void Parse_NonMTP_ShouldSuccess()
        {
            var block = Block.Parse(BlockData.NonMtp, ZcoinNetworks.Instance.Mainnet);

            Assert.Equal(uint256.Parse("3c69e19de9889590adbb365cfa20acab64ab67eccdfea29aed735275dd027734"), block.GetHash());
            Assert.False(block.Header.IsMtp());
        }

        [Fact]
        public void Parse_WithMTP_ShouldSuccess()
        {
            var block = Block.Parse(BlockData.Mtp, ZcoinNetworks.Instance.Mainnet);

            Assert.Equal(uint256.Parse("f3646083759fd81d6cddeb6667deddde6745c77346ac81d58f282162763bc8aa"), block.GetHash());
            Assert.True(block.Header.IsMtp());
        }
    }
}
