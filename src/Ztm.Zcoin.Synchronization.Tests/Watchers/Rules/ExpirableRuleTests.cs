using System;
using Xunit;
using Ztm.Zcoin.Synchronization.Watchers.Rules;

namespace Ztm.Zcoin.Synchronization.Tests.Watchers.Rules
{
    public class ExpirableRuleTests
    {
        readonly ExpirableRule subject;

        public ExpirableRuleTests()
        {
            this.subject = new ExpirableRule();
        }

        [Fact]
        public void Constructor_WithoutId_ShouldGenerate()
        {
            var subject = new ExpirableRule();

            Assert.NotEqual(Guid.Empty, subject.Id);
        }

        [Fact]
        public void Constructor_WithId_ShouldUseThat()
        {
            var id = Guid.NewGuid();
            var subject = new ExpirableRule(id);

            Assert.Equal(id, subject.Id);
        }

        [Fact]
        public void Equals_BothDifferentExpirePolicy_ShouldReturnFalse()
        {
            var subject = new ExpirableRule(this.subject.Id);

            this.subject.ExpirePolicy = new TestExpirePolicy();
            subject.ExpirePolicy = new TestExpirePolicy();

            Assert.NotEqual(subject, this.subject);
        }

        [Fact]
        public void Equals_BothSameExpirePolicy_ShouldReturnTrue()
        {
            var expirePolicy = new TestExpirePolicy();
            var subject = new ExpirableRule(this.subject.Id);

            this.subject.ExpirePolicy = expirePolicy;
            subject.ExpirePolicy = expirePolicy;

            Assert.Equal(subject, this.subject);
        }

        [Fact]
        public void Equals_BothNullExpirePolicy_ShouldReturnTrue()
        {
            var subject = new ExpirableRule(this.subject.Id);

            Assert.Equal(subject, this.subject);
        }
    }
}
