using System;
using Xunit;
using Ztm.Testing;
using Ztm.Threading;
using Ztm.WebApi.Watchers.TokenBalance;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.WebApi.Tests.Watchers.TokenBalance
{
    public sealed class WatchingInfoTests
    {
        readonly Rule rule;
        readonly Timer timer;
        readonly WatchingInfo subject;

        public WatchingInfoTests()
        {
            this.rule = new Rule(
                new PropertyId(3),
                TestAddress.Regtest1,
                new PropertyAmount(100),
                6,
                TimeSpan.FromHours(1),
                "timeout",
                Guid.NewGuid());
            this.timer = new Timer();
            this.subject = new WatchingInfo(this.rule, this.timer);
        }

        [Fact]
        public void Constructor_WithNullRule_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>("rule", () => new WatchingInfo(null, this.timer));
        }

        [Fact]
        public void Constructor_WithNullTimer_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>("timer", () => new WatchingInfo(this.rule, null));
        }

        [Fact]
        public void Constructor_WhenSucceeded_ShouldInitializeProperties()
        {
            Assert.Equal(this.rule, this.subject.Rule);
            Assert.Equal(this.timer, this.subject.Timer);
        }

        [Fact]
        public void Deconstruct_ToRuleAndTimer_ShouldOutputRuleAndTimer()
        {
            var (rule, timer) = this.subject;

            Assert.Equal(this.subject.Rule, rule);
            Assert.Equal(this.subject.Timer, timer);
        }
    }
}
