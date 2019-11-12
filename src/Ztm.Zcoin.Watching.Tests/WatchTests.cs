using System;
using NBitcoin;
using Xunit;

namespace Ztm.Zcoin.Watching.Tests
{
    public sealed class WatchTests
    {
        readonly Watch<object> subject;

        public WatchTests()
        {
            this.subject = new Watch<object>(null, uint256.One);
        }

        [Fact]
        public void Constructor_WithNullStartBlock_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>("startBlock", () => new Watch<object>(null, null));
            Assert.Throws<ArgumentNullException>("startBlock", () => new Watch<object>(null, null, DateTime.Now));
            Assert.Throws<ArgumentNullException>("startBlock", () => new Watch<object>(null, null, DateTime.Now, Guid.NewGuid()));
        }

        [Fact]
        public void Constructor_WithNullContext_ShouldSuccess()
        {
            var subject = new Watch<object>(null, uint256.One);

            Assert.Null(subject.Context);
        }

        [Fact]
        public void Constructor_WithoutStartTime_ShouldUseCurrentLocalTime()
        {
            var subject = new Watch<object>(null, uint256.One);

            Assert.Equal(uint256.One, subject.StartBlock);
            Assert.True(subject.StartTime > DateTime.Now - new TimeSpan(0, 0, 1));
        }

        [Fact]
        public void Constructor_WithoutId_ShouldGenerate()
        {
            Assert.NotEqual(Guid.Empty, new Watch<object>(null, uint256.One).Id);
            Assert.NotEqual(Guid.Empty, new Watch<object>(null, uint256.One, DateTime.Now).Id);
        }

        [Fact]
        public void Constructor_WithValidArgument_ShouldInitializeProperties()
        {
            var id = Guid.NewGuid();
            var context = new object();
            var subject = new Watch<object>(context, uint256.One, new DateTime(2019, 6, 25), id);

            Assert.Equal(id, subject.Id);
            Assert.Same(context, subject.Context);
            Assert.Equal(uint256.One, subject.StartBlock);
            Assert.Equal(new DateTime(2019, 6, 25), subject.StartTime);
        }

        [Fact]
        public void Equals_WithNull_ShouldReturnFalse()
        {
            Assert.False(this.subject.Equals(null));
        }

        [Fact]
        public void Equals_WithDifferentType_ShouldReturnFalse()
        {
            var input = new DerivedWatch(
                this.subject.Context,
                this.subject.StartBlock,
                this.subject.StartTime,
                this.subject.Id
            );

            Assert.False(this.subject.Equals(input));
        }

        [Fact]
        public void Equals_WithDifferentId_ShouldReturnFalse()
        {
            var input = new Watch<object>(null, this.subject.StartBlock, this.subject.StartTime, Guid.NewGuid());

            Assert.False(this.subject.Equals(input));
        }

        [Fact]
        public void Equals_WithSameId_ShouldReturnTrue()
        {
            var input = new Watch<object>(null, this.subject.StartBlock, this.subject.StartTime, this.subject.Id);

            Assert.True(this.subject.Equals(input));
        }

        [Fact]
        public void GetHashCode_WhenInvoke_ShouldReturnTheSameAsIdHash()
        {
            Assert.Equal(this.subject.Id.GetHashCode(), this.subject.GetHashCode());
        }

        sealed class DerivedWatch : Watch<object>
        {
            public DerivedWatch(object context, uint256 startBlock, DateTime startTime, Guid id)
                : base(context, startBlock, startTime, id)
            {
            }
        }
    }
}
