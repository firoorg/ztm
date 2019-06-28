using System;
using NBitcoin;
using Xunit;
using Ztm.Zcoin.Synchronization.Watchers.Rules;

namespace Ztm.Zcoin.Synchronization.Tests.Watchers.Rules
{
    public class RuledWatchTests
    {
        readonly RuledWatch<Rule> subject;

        public RuledWatchTests()
        {
            this.subject = new RuledWatch<Rule>(new Rule(), uint256.One);
        }

        [Fact]
        public void Constructor_WithNullRule_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>("rule", () => new RuledWatch<Rule>(null, uint256.One));
            Assert.Throws<ArgumentNullException>("rule", () => new RuledWatch<Rule>(null, uint256.One, DateTime.Now));
            Assert.Throws<ArgumentNullException>("rule", () => new RuledWatch<Rule>(null, uint256.One, DateTime.Now, Guid.NewGuid()));
        }

        [Fact]
        public void Constructor_WithNullStartBlock_ShouldThrow()
        {
            var rule = new Rule();

            Assert.Throws<ArgumentNullException>("startBlock", () => new RuledWatch<Rule>(rule, null));
            Assert.Throws<ArgumentNullException>("startBlock", () => new RuledWatch<Rule>(rule, null, DateTime.Now));
            Assert.Throws<ArgumentNullException>("startBlock", () => new RuledWatch<Rule>(rule, null, DateTime.Now, Guid.NewGuid()));
        }

        [Fact]
        public void Constructor_WithoutStartTime_ShouldUseCurrentLocalTime()
        {
            var rule = new Rule();
            var subject = new RuledWatch<Rule>(rule, uint256.One);

            Assert.Equal(uint256.One, subject.StartBlock);
            Assert.True(subject.StartTime > DateTime.Now - new TimeSpan(0, 0, 1));
        }

        [Fact]
        public void Constructor_WithoutId_ShouldGenerate()
        {
            var rule = new Rule();

            Assert.NotEqual(Guid.Empty, new RuledWatch<Rule>(rule, uint256.One).Id);
            Assert.NotEqual(Guid.Empty, new RuledWatch<Rule>(rule, uint256.One, DateTime.Now).Id);
        }

        [Fact]
        public void Constructor_WithValidArgument_ShouldInitializeProperties()
        {
            var rule = new Rule();
            var id = Guid.NewGuid();
            var subject = new RuledWatch<Rule>(rule, uint256.One, new DateTime(2019, 6, 25), id);

            Assert.Same(rule, subject.Rule);
            Assert.Equal(uint256.One, subject.StartBlock);
            Assert.Equal(new DateTime(2019, 6, 25), subject.StartTime);
            Assert.Equal(id, subject.Id);
        }

        [Fact]
        public void Equals_WithNull_ShouldReturnFalse()
        {
            Assert.False(this.subject.Equals(null));
        }

        [Fact]
        public void Equals_WithDifferentType_ShouldReturnFalse()
        {
            var input = new DerivedRuledWatch(
                this.subject.Rule,
                this.subject.StartBlock,
                this.subject.StartTime,
                this.subject.Id
            );

            Assert.False(this.subject.Equals(input));
        }

        [Fact]
        public void Equals_WithDifferentId_ShouldReturnFalse()
        {
            var input = new RuledWatch<Rule>(
                this.subject.Rule,
                this.subject.StartBlock,
                this.subject.StartTime,
                Guid.NewGuid()
            );

            Assert.False(this.subject.Equals(input));
        }

        [Fact]
        public void Equals_WithDifferentStartBlock_ShouldReturnFalse()
        {
            var input = new RuledWatch<Rule>(this.subject.Rule, uint256.Zero, this.subject.StartTime, this.subject.Id);

            Assert.False(this.subject.Equals(input));
        }

        [Fact]
        public void Equals_WithDifferentStartTime_ShouldReturnFalse()
        {
            var input = new RuledWatch<Rule>(this.subject.Rule, this.subject.StartBlock, DateTime.Now, this.subject.Id);

            Assert.False(this.subject.Equals(input));
        }

        [Fact]
        public void Equals_WithDifferentRule_ShouldReturnFalse()
        {
            var input = new RuledWatch<Rule>(
                new Rule(),
                this.subject.StartBlock,
                this.subject.StartTime,
                this.subject.Id
            );

            Assert.False(this.subject.Equals(input));
        }

        [Fact]
        public void Equals_WithSameProperties_ShouldReturnTrue()
        {
            var input = new RuledWatch<Rule>(
                this.subject.Rule,
                this.subject.StartBlock,
                this.subject.StartTime,
                this.subject.Id
            );

            Assert.True(this.subject.Equals(input));
        }

        class DerivedRuledWatch : RuledWatch<Rule>
        {
            public DerivedRuledWatch(Rule rule, uint256 startBlock)
                : base(rule, startBlock)
            {
            }

            public DerivedRuledWatch(Rule rule, uint256 startBlock, DateTime startTime)
                : base(rule, startBlock, startTime)
            {
            }

            public DerivedRuledWatch(Rule rule, uint256 startBlock, DateTime startTime, Guid id)
                : base(rule, startBlock, startTime, id)
            {
            }
        }
    }
}
