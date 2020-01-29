using System;
using System.Collections.Generic;
using NBitcoin;
using Xunit;
using Ztm.Testing;

namespace Ztm.Zcoin.Watching.Tests
{
    public sealed class BalanceConfirmationTests
    {
        readonly Dictionary<BalanceWatch<object, int>, int> watches;
        readonly BalanceConfirmation<object, int> subject;

        public BalanceConfirmationTests()
        {
            this.watches = new Dictionary<BalanceWatch<object, int>, int>()
            {
                { new BalanceWatch<object, int>(new object(), uint256.One, uint256.One, TestAddress.Regtest1, 1), 1 }
            };
            this.subject = new BalanceConfirmation<object, int>(uint256.One, TestAddress.Regtest1, this.watches);
        }

        [Fact]
        public void Constructor_WithNullBlock_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "block",
                () => new BalanceConfirmation<object, int>(null, TestAddress.Regtest1, this.watches)
            );
        }

        [Fact]
        public void Constructor_WithNullAddress_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "address",
                () => new BalanceConfirmation<object, int>(uint256.One, null, this.watches)
            );
        }

        [Fact]
        public void Constructor_WithNullWatches_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "watches",
                () => new BalanceConfirmation<object, int>(uint256.One, TestAddress.Regtest1, null)
            );
        }

        [Fact]
        public void Constructor_WithEmptyWatches_ShouldThrow()
        {
            Assert.Throws<ArgumentException>(
                "watches",
                () => new BalanceConfirmation<object, int>(
                    uint256.One,
                    TestAddress.Regtest1,
                    new Dictionary<BalanceWatch<object, int>, int>()
                )
            );
        }

        [Fact]
        public void Constructor_WhenSuccess_ShouldInitializeProperties()
        {
            Assert.Equal(TestAddress.Regtest1, this.subject.Address);
            Assert.Equal(uint256.One, this.subject.Block);
            Assert.Equal(this.watches, this.subject.Watches);
        }
    }
}
