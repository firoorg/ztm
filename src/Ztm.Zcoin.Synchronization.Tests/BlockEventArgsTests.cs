using System;
using System.Threading;
using Xunit;
using Ztm.Zcoin.NBitcoin;

namespace Ztm.Zcoin.Synchronization.Tests
{
    public class BlockEventArgsTests
    {
        [Fact]
        public void Constructor_PassNullForBlock_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "block",
                () => new BlockEventArgs(null, 0, CancellationToken.None)
            );
        }

        [Fact]
        public void Constructor_PassNegativeForHeight_ShouldThrow()
        {
            var block = (ZcoinBlock)ZcoinNetworks.Instance.Regtest.GetGenesis();

            Assert.Throws<ArgumentOutOfRangeException>(
                "height",
                () => new BlockEventArgs(block, -1, CancellationToken.None)
            );
        }

        [Fact]
        public void Constructor_PassValidParameters_ShouldInitProperties()
        {
            var block = (ZcoinBlock)ZcoinNetworks.Instance.Regtest.GetGenesis();
            var subject = new BlockEventArgs(block, 1, CancellationToken.None);

            Assert.Same(block, subject.Block);
            Assert.Equal(1, subject.Height);
        }
    }
}
