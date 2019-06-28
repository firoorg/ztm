using System;
using Xunit;
using Ztm.Zcoin.Synchronization.Watchers.Rules;

namespace Ztm.Zcoin.Synchronization.Tests.Watchers.Rules
{
    public class RuleTests
    {
        readonly Rule subject;

        public RuleTests()
        {
            this.subject = new Rule();
        }

        [Fact]
        public void Constructor_WithoutId_ShouldGenerate()
        {
            var subject = new Rule();

            Assert.NotEqual(Guid.Empty, subject.Id);
        }

        [Fact]
        public void Constructor_WithId_ShouldUseThatId()
        {
            var id = Guid.NewGuid();
            var subject = new Rule(id);

            Assert.Equal(id, subject.Id);
        }

        [Fact]
        public void Equals_OtherIsNull_ShouldReturnFalse()
        {
            Assert.False(this.subject.Equals(null));
        }

        [Fact]
        public void Equals_OtherIsDifferentType_ShouldReturnFalse()
        {
            var other = new DerivedRule(this.subject.Id);

            Assert.False(this.subject.Equals(other));
        }

        [Fact]
        public void Equals_OtherHaveSameId_ShouldReturnTrue()
        {
            var other = new Rule(this.subject.Id);

            Assert.True(this.subject.Equals(other));
        }

        class DerivedRule : Rule
        {
            public DerivedRule(Guid id) : base(id)
            {
            }
        }
    }
}
