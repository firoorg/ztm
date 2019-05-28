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
                () => new BlockEventArgs(null, CancellationToken.None)
            );
        }

        [Fact]
        public void Constructor_PassNonNullForBlock_ShouldAssignToBlock()
        {
            var block = (ZcoinBlock)ZcoinNetworks.Instance.Regtest.GetGenesis();
            var subject = new BlockEventArgs(block, CancellationToken.None);

            Assert.Same(block, subject.Block);
        }
    }
}
