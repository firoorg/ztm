using System;
using NBitcoin;
using Xunit;
using Ztm.Testing;
using Ztm.Zcoin.NBitcoin.Exodus;
using Ztm.Zcoin.NBitcoin.Exodus.TransactionRetrievers;

namespace Ztm.Zcoin.NBitcoin.Tests.Exodus.TransactionRetrievers
{
    public sealed class BalanceChangeTests
    {
        readonly BitcoinAddress address;
        readonly PropertyAmount amount;
        readonly PropertyId property;
        readonly BalanceChange subject;

        public BalanceChangeTests()
        {
            this.address = TestAddress.Mainnet1;
            this.amount = new PropertyAmount(10);
            this.property = new PropertyId(2);

            this.subject = new BalanceChange(address, amount, property);
        }

        [Fact]
        public void Construct_WithNullArgs_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "address",
                () => new BalanceChange(null, amount, property)
            );

            Assert.Throws<ArgumentNullException>(
                "property",
                () => new BalanceChange(address, amount, null)
            );
        }

        [Fact]
        public void Construct_WithValidArgs_ShouldSuccess()
        {
            // Arrange.
            var address = TestAddress.Mainnet1;
            var amount = new PropertyAmount(10);
            var property = new PropertyId(2);

            // Act.
            var c = new BalanceChange(address, amount, property);

            // Assert.
            Assert.Equal(address, c.Address);
            Assert.Equal(amount, c.Amount);
            Assert.Equal(property, c.Property);
        }

        [Fact]
        public void Equals_WithNull_ShouldReturnFalse()
        {
            Assert.False(this.subject.Equals((BalanceChange)null));
        }

        [Fact]
        public void Equals_WithUnequalValue_ShouldReturnFalse()
        {
            Assert.False(this.subject.Equals(new BalanceChange(TestAddress.Regtest1, amount, property)));
            Assert.False(this.subject.Equals(new BalanceChange(address, new PropertyAmount(11), property)));
            Assert.False(this.subject.Equals(new BalanceChange(address, amount, new PropertyId(3))));
        }

        [Fact]
        public void Equals_WithEqualValue_ShouldReturnTrue()
        {
            Assert.True(this.subject.Equals(new BalanceChange(address, amount, property)));
        }

        [Fact]
        public void Equals_WithObjectWithNullValue_ShouldReturnFalse()
        {
            Assert.False(this.subject.Equals((object)null));
        }

        [Fact]
        public void Equals_WithObjectWithOtherType_ShouldReturnFalse()
        {
            Assert.False(this.subject.Equals((object)(new string(""))));
        }

        [Fact]
        public void Equals_WithObjectWithSameTypeButUnequalValue_ShouldReturnFalse()
        {
            Assert.False(this.subject.Equals((object)(new BalanceChange(TestAddress.Regtest1, amount, property))));
            Assert.False(this.subject.Equals((object)(new BalanceChange(address, new PropertyAmount(11), property))));
            Assert.False(this.subject.Equals((object)(new BalanceChange(address, amount, new PropertyId(3)))));
        }

        [Fact]
        public void Equals_WithObjectWithSameTypeAndEqualValue_ShouldReturnTrue()
        {
            Assert.True(this.subject.Equals((object)(new BalanceChange(address, amount, property))));
        }
    }
}