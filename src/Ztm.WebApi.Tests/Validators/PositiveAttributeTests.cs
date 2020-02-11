using System;
using NBitcoin;
using Xunit;
using Ztm.WebApi.Validators;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.WebApi.Tests.Validators
{
    public sealed class PositiveAttributeTests
    {
        readonly PositiveAttribute subject;

        public PositiveAttributeTests()
        {
            this.subject = new PositiveAttribute();
        }

        [Fact]
        public void IsValid_WithNull_ShouldReturnTrue()
        {
            var result = this.subject.IsValid(null);

            Assert.True(result);
        }

        [Fact]
        public void IsValid_WithNullPropertyAmount_ShouldReturnTrue()
        {
            var result = this.subject.IsValid(default(PropertyAmount?));

            Assert.True(result);
        }

        [Theory]
        [InlineData(long.MinValue)]
        [InlineData(-1)]
        public void IsValid_WithNegativePropertyAmount_ShouldReturnFalse(long amount)
        {
            var result = this.subject.IsValid(new PropertyAmount(amount));

            Assert.False(result);
        }

        [Fact]
        public void IsValid_WithZeroPropertyAmount_ShouldReturnFalse()
        {
            var result = this.subject.IsValid(PropertyAmount.Zero);

            Assert.False(result);
        }

        [Theory]
        [InlineData(1L)]
        [InlineData(long.MaxValue)]
        public void IsValid_WithPositivePropertyAmount_ShouldReturnTrue(long amount)
        {
            var result = this.subject.IsValid(new PropertyAmount(amount));

            Assert.True(result);
        }

        [Fact]
        public void IsValid_WithNegativeMoney_ShouldReturnFalse()
        {
            var result = this.subject.IsValid(new Money(-1L));

            Assert.False(result);
        }

        [Fact]
        public void IsValid_WithZeroMoney_ShouldReturnFalse()
        {
            var result = this.subject.IsValid(Money.Zero);

            Assert.False(result);
        }

        [Theory]
        [InlineData(1L)]
        [InlineData(long.MaxValue)]
        public void IsValid_WithPositiveMoney_ShouldReturnTrue(long satoshi)
        {
            var result = this.subject.IsValid(new Money(satoshi));

            Assert.True(result);
        }

        [Theory]
        [InlineData(1f)]
        [InlineData(1d)]
        [InlineData((byte)1)]
        [InlineData((short)1)]
        [InlineData(1)]
        [InlineData(1L)]
        [InlineData("1")]
        public void IsValid_WithUnsupportedType_ShouldThrow(object value)
        {
            Assert.Throws<ArgumentException>("value", () => this.subject.IsValid(value));
        }
    }
}
