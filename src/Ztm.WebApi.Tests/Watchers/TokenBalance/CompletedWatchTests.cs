using System;
using NBitcoin;
using Xunit;
using Ztm.Testing;
using Ztm.WebApi.Watchers.TokenBalance;
using Ztm.Zcoin.NBitcoin.Exodus;
using Watch = Ztm.Zcoin.Watching.BalanceWatch<Ztm.WebApi.Watchers.TokenBalance.Rule, Ztm.Zcoin.NBitcoin.Exodus.PropertyAmount>;

namespace Ztm.WebApi.Tests.Watchers.TokenBalance
{
    public sealed class CompletedWatchTests
    {
        readonly Rule rule;
        readonly Watch watch;
        readonly CompletedWatch subject;

        public CompletedWatchTests()
        {
            this.rule = new Rule(
                new PropertyId(3),
                TestAddress.Regtest1,
                new PropertyAmount(100),
                6,
                TimeSpan.FromHours(1),
                "timeout",
                Guid.NewGuid());
            this.watch = new Watch(
                this.rule,
                uint256.One,
                uint256.One,
                TestAddress.Regtest1,
                new PropertyAmount(50));
            this.subject = new CompletedWatch(this.watch, 5);
        }

        [Fact]
        public void Constructor_WithNullWatch_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>("watch", () => new CompletedWatch(null, 0));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-2)]
        public void Constructor_WithNegativeConfirmation_ShouldThrow(int confirmation)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                "confirmation",
                () => new CompletedWatch(this.watch, confirmation));
        }

        [Fact]
        public void Constructor_WhenSucceeded_ShouldInitializeProperties()
        {
            Assert.Equal(5, this.subject.Confirmation);
            Assert.Equal(this.watch, this.subject.Watch);
        }
    }
}
