using System;
using NBitcoin;
using Xunit;
using Ztm.Zcoin.Synchronization.Watchers.Rules;

namespace Ztm.Zcoin.Synchronization.Tests.Watchers.Rules
{
    public class TransactionRuleTests
    {
        readonly TransactionRule subject;

        public TransactionRuleTests()
        {
            this.subject = new TransactionRule(uint256.One);
        }

        [Fact]
        public void Constructor_WithNullTransactionHash_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "transactionHash",
                () => new TransactionRule(null)
            );

            Assert.Throws<ArgumentNullException>(
                "transactionHash",
                () => new TransactionRule(null, Guid.NewGuid())
            );
        }

        [Fact]
        public void Equals_WithDifferentTransactionHash_ShouldReturnFalse()
        {
            var other = new TransactionRule(uint256.Zero, this.subject.Id);

            Assert.False(this.subject.Equals(other));
        }

        [Fact]
        public void Equals_WithEqualTransactionHash_ShouldReturnTrue()
        {
            var other = new TransactionRule(this.subject.TransactionHash, this.subject.Id);

            Assert.True(this.subject.Equals(other));
        }
    }
}
