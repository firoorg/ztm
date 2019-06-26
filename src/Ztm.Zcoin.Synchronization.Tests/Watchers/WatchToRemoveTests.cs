using System;
using NBitcoin;
using Xunit;
using Ztm.Zcoin.Synchronization.Watchers;

namespace Ztm.Zcoin.Synchronization.Tests.Watchers
{
    public class WatchToRemoveTests
    {
        readonly Watch watch;
        readonly WatchToRemove<Watch> subject;

        public WatchToRemoveTests()
        {
            this.watch = new Watch(uint256.One);
            this.subject = new WatchToRemove<Watch>(
                this.watch,
                WatchRemoveReason.Completed | WatchRemoveReason.BlockRemoved
            );
        }

        [Fact]
        public void Constructor_WithNullWatch_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "watch",
                () => new WatchToRemove<Watch>(null, WatchRemoveReason.Completed)
            );
        }

        [Fact]
        public void Constructor_WithValidArguments_ShouldInitializeProperties()
        {
            var watch = new Watch(uint256.One);
            var subject = new WatchToRemove<Watch>(watch, WatchRemoveReason.Completed);

            Assert.Equal(WatchRemoveReason.Completed, subject.Reason);
            Assert.Same(watch, subject.Watch);
        }

        [Fact]
        public void Equals_WithNull_ShouldReturnFalse()
        {
            Assert.False(this.subject.Equals((WatchToRemove<Watch>)null));
            Assert.False(this.subject.Equals((object)null));
        }

        [Fact]
        public void Equals_WithDifferentType_ShouldReturnFalse()
        {
            var input = new WatchToRemove<DerivedWatch>(
                new DerivedWatch(this.subject.Watch.StartBlock, this.subject.Watch.StartTime),
                this.subject.Reason
            );

            Assert.False(this.subject.Equals(input));
            Assert.False(this.subject.Equals((object)input));
        }

        [Theory]
        [InlineData(WatchRemoveReason.None)]
        [InlineData(WatchRemoveReason.Completed)]
        [InlineData(WatchRemoveReason.BlockRemoved)]
        public void Equals_WithDifferentReason_ShouldReturnFalse(WatchRemoveReason reason)
        {
            var input = new WatchToRemove<Watch>(this.subject.Watch, reason);

            Assert.False(this.subject.Equals(input));
            Assert.False(this.subject.Equals((object)input));
        }

        [Fact]
        public void Equals_WithDifferentWatch_ShouldReturnFalse()
        {
            var input = new WatchToRemove<Watch>(new Watch(uint256.Zero), this.subject.Reason);

            Assert.False(this.subject.Equals(input));
            Assert.False(this.subject.Equals((object)input));
        }

        [Fact]
        public void Equals_WithSameReasonAndWatch_ShouldReturnTrue()
        {
            var input = new WatchToRemove<Watch>(this.subject.Watch, this.subject.Reason);

            Assert.True(this.subject.Equals(input));
            Assert.True(this.subject.Equals((object)input));
        }

        class DerivedWatch : Watch
        {
            public DerivedWatch(uint256 startBlock, DateTime startTime) : base(startBlock, startTime)
            {
            }
        }
    }
}
