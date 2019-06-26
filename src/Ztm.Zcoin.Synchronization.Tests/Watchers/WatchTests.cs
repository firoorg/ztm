using System;
using NBitcoin;
using Xunit;
using Ztm.Zcoin.Synchronization.Watchers;

namespace Ztm.Zcoin.Synchronization.Tests.Watchers
{
    public class WatchTests
    {
        readonly Watch subject;

        public WatchTests()
        {
            this.subject = new Watch(uint256.One);
        }

        [Fact]
        public void Constructor_WithNullStartBlock_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>("startBlock", () => new Watch(null));
        }

        [Fact]
        public void Constructor_WithoutStartTime_ShouldUseCurrentLocalTime()
        {
            var subject = new Watch(uint256.One);

            Assert.Equal(uint256.One, subject.StartBlock);
            Assert.True(subject.StartTime > DateTime.Now - new TimeSpan(0, 0, 1));
        }

        [Fact]
        public void Constructor_WithoutId_ShouldGenerate()
        {
            Assert.NotEqual(Guid.Empty, new Watch(uint256.One).Id);
            Assert.NotEqual(Guid.Empty, new Watch(uint256.One, DateTime.Now).Id);
        }

        [Fact]
        public void Constructor_WithValidArgument_ShouldInitializeProperties()
        {
            var id = Guid.NewGuid();
            var subject = new Watch(uint256.One, new DateTime(2019, 6, 25), id);

            Assert.Equal(id, subject.Id);
            Assert.Equal(uint256.One, subject.StartBlock);
            Assert.Equal(new DateTime(2019, 6, 25), subject.StartTime);
        }

        [Fact]
        public void Equals_WithNull_ShouldReturnFalse()
        {
            Assert.False(this.subject.Equals(null));
            Assert.False(this.subject.Equals((object)null));
        }

        [Fact]
        public void Equals_WithDifferentType_ShouldReturnFalse()
        {
            var input = new DerivedWatch(this.subject.StartBlock, this.subject.StartTime, this.subject.Id);

            Assert.False(this.subject.Equals(input));
            Assert.False(this.subject.Equals((object)input));
        }

        [Fact]
        public void Equals_WithDifferentId_ShouldReturnFalse()
        {
            var input = new Watch(this.subject.StartBlock, this.subject.StartTime, Guid.NewGuid());

            Assert.False(this.subject.Equals(input));
            Assert.False(this.subject.Equals((object)input));
        }

        [Fact]
        public void Equals_WithDifferentStartBlock_ShouldReturnFalse()
        {
            var input = new Watch(uint256.Zero, this.subject.StartTime, this.subject.Id);

            Assert.False(this.subject.Equals(input));
            Assert.False(this.subject.Equals((object)input));
        }

        [Fact]
        public void Equals_WithDifferentStartTime_ShouldReturnFalse()
        {
            var input = new Watch(this.subject.StartBlock, DateTime.Now, this.subject.Id);

            Assert.False(this.subject.Equals(input));
            Assert.False(this.subject.Equals((object)input));
        }

        [Fact]
        public void Equals_WithSameIdAndStartBlockAndStartTime_ShouldReturnTrue()
        {
            var input = new Watch(this.subject.StartBlock, this.subject.StartTime, this.subject.Id);

            Assert.True(this.subject.Equals(input));
            Assert.True(this.subject.Equals((object)input));
        }

        class DerivedWatch : Watch
        {
            public DerivedWatch(uint256 startBlock, DateTime startTime, Guid id) : base(startBlock, startTime, id)
            {
            }
        }
    }
}
