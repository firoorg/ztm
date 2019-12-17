using System;
using Xunit;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.Zcoin.NBitcoin.Tests.Exodus
{
    public sealed class PropertyAmountTests
    {
        [Theory]
        [InlineData(long.MinValue)]
        [InlineData(long.MaxValue)]
        [InlineData(0L)]
        public void Constructor_WithValidValue_ShouldSuccess(long value)
        {
            new PropertyAmount(value);
        }

        [Theory]
        [InlineData(-9223372036854775808L, "-92233720368.54775808")]
        [InlineData(-1, "-0.00000001")]
        [InlineData(1L, "0.00000001")]
        [InlineData(100000000L, "1.00000000")]
        [InlineData(9223372036854775807, "92233720368.54775807")]
        public void Divisible_GetValue_ShouldReturnOriginalValueDivideBy100000000(long value, string expect)
        {
            var amount = new PropertyAmount(value);

            Assert.Equal(decimal.Parse(expect), amount.Divisible);
        }

        [Theory]
        [InlineData(-9223372036854775808L)]
        [InlineData(-1)]
        [InlineData(1L)]
        [InlineData(100000000L)]
        [InlineData(9223372036854775807)]
        public void Indivisible_GetValue_ShouldReturnOriginalValue(long value)
        {
            var amount = new PropertyAmount(value);

            Assert.Equal(value, amount.Indivisible);
        }

        [Theory]
        [InlineData("-92233720368.54775808")]
        [InlineData("0.00000001")]
        [InlineData("0")]
        [InlineData("92233720368.54775807")]
        public void FromDivisible_WithValidValue_ShouldSuccess(string s)
        {
            var value = decimal.Parse(s);
            var amount = PropertyAmount.FromDivisible(value);

            Assert.Equal(value, amount.Divisible);
            Assert.Equal((long)(value * 100000000), amount.Indivisible);
        }

        [Theory]
        [InlineData("0.000000009")]
        [InlineData("-92233720368.54775809")]
        [InlineData("92233720368.54775808")]
        public void FromDivisible_WithInvalidValue_ShouldThrow(string s)
        {
            var v = decimal.Parse(s);

            Assert.Throws<ArgumentException>(() => PropertyAmount.FromDivisible(v));
        }

        [Fact]
        public void Parse_WithNull_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>("s", () => PropertyAmount.Parse(null));
        }

        [Theory]
        [InlineData("-92233720368.54775808", -9223372036854775808L)]
        [InlineData("-0.00000001", -1L)]
        [InlineData("0.00000001", 1L)]
        [InlineData("0.1", 10000000L)]
        [InlineData("0.0", 0L)]
        [InlineData("1.0", 100000000L)]
        [InlineData("1.9", 190000000L)]
        [InlineData("92233720368.54775807", 9223372036854775807L)]
        public void Parse_WithValidDivisible_ShouldSuccess(string divisible, long indivisible)
        {
            var amount = PropertyAmount.Parse(divisible);

            Assert.Equal(decimal.Parse(divisible), amount.Divisible);
            Assert.Equal(indivisible, amount.Indivisible);
        }

        [Theory]
        [InlineData("")]
        [InlineData("a.0")]
        [InlineData("1a.1")]
        [InlineData("1.a")]
        [InlineData("1.1a")]
        [InlineData("0.000000001")]
        public void Parse_WithInvalidDivisible_ShouldThrow(string s)
        {
            Assert.Throws<FormatException>(() => PropertyAmount.Parse(s));
        }

        [Theory]
        [InlineData("-92233720368.54775809")]
        [InlineData("92233720368.54775808")]
        public void Parse_WithDivisibleOutOfRange_ShouldThrow(string s)
        {
            Assert.Throws<OverflowException>(() => PropertyAmount.Parse(s));
        }

        [Theory]
        [InlineData("-9223372036854775808", "-92233720368.54775808")]
        [InlineData("-1", "-0.00000001")]
        [InlineData("0", "0.0")]
        [InlineData("9223372036854775807", "92233720368.54775807")]
        public void Parse_WithValidIndivisible_ShouldSuccess(string indivisible, string divisible)
        {
            var amount = PropertyAmount.Parse(indivisible);

            Assert.Equal(long.Parse(indivisible), amount.Indivisible);
            Assert.Equal(decimal.Parse(divisible), amount.Divisible);
        }

        [Theory]
        [InlineData("")]
        [InlineData("aa")]
        [InlineData("1a")]
        public void Parse_WithInvalidIndivisible_ShouldThrow(string s)
        {
            Assert.Throws<FormatException>(() => PropertyAmount.Parse(s));
        }

        [Theory]
        [InlineData("-9223372036854775809")]
        [InlineData("9223372036854775808")]
        public void Parse_WithIndivisibleOutOfRange_ShouldThrow(string s)
        {
            Assert.Throws<OverflowException>(() => PropertyAmount.Parse(s));
        }

        [Theory]
        [InlineData(-2L, -1L)]
        [InlineData(-1L, 0L)]
        [InlineData(0L, 1L)]
        public void Equals_WithValueLowerThan_ShouldReturnFalse(long left, long right)
        {
            var first = new PropertyAmount(left);
            var second = new PropertyAmount(right);

            Assert.False(first.Equals(second));
            Assert.False(first.Equals((object)second));
        }

        [Theory]
        [InlineData(-1L)]
        [InlineData(0L)]
        [InlineData(1L)]
        public void Equals_WithSameValue_ShouldReturnTrue(long value)
        {
            var first = new PropertyAmount(value);
            var second = new PropertyAmount(value);

            Assert.True(first.Equals(second));
            Assert.True(first.Equals((object)second));
        }

        [Theory]
        [InlineData(-1L, -2L)]
        [InlineData(0L, -1L)]
        [InlineData(1L, 0L)]
        public void Equals_WithValueGreaterThan_ShouldReturnFalse(long left, long right)
        {
            var first = new PropertyAmount(left);
            var second = new PropertyAmount(right);

            Assert.False(first.Equals(second));
            Assert.False(first.Equals((object)second));
        }

        [Fact]
        public void Equals_WithNull_ShouldReturnFalse()
        {
            var first = new PropertyAmount(0);

            Assert.False(first.Equals(null));
        }

        [Fact]
        public void Equals_WithDifferentType_ShouldReturnFalse()
        {
            var first = new PropertyAmount(0);

            Assert.False(first.Equals(0L));
        }

        [Theory]
        [InlineData(-9223372036854775808L, "-92233720368.54775808")]
        [InlineData(-1L, "-0.00000001")]
        [InlineData(0L, "0.00000000")]
        [InlineData(1L, "0.00000001")]
        [InlineData(100000000L, "1.00000000")]
        [InlineData(9223372036854775807L, "92233720368.54775807")]
        public void ToString_WithDivisible_ShouldSuccess(long value, string expect)
        {
            var amount = new PropertyAmount(value);

            Assert.Equal(expect, amount.ToString(PropertyType.Divisible));
        }

        [Theory]
        [InlineData(-9223372036854775808L)]
        [InlineData(-1L)]
        [InlineData(0L)]
        [InlineData(1L)]
        [InlineData(100000000L)]
        [InlineData(9223372036854775807L)]
        public void ToString_WithIndivisible_ShouldSuccess(long value)
        {
            var amount = new PropertyAmount(value);

            Assert.Equal(value.ToString(), amount.ToString(PropertyType.Indivisible));
        }

        [Fact]
        public void ToString_WithInvalidType_ShouldThrow()
        {
            var amount = new PropertyAmount(0);

            Assert.Throws<ArgumentException>("type", () => amount.ToString((PropertyType)100));
        }

        [Fact]
        public void ToString_WithoutArgument_ShouldReturnFullTypeName()
        {
            var amount = new PropertyAmount(1);

            Assert.Equal(typeof(PropertyAmount).FullName, amount.ToString());
        }

        [Theory]
        [InlineData(-2L, -1L)]
        [InlineData(-1L, 0L)]
        [InlineData(0L, 1L)]
        public void LessThan_WithLess_ShouldReturnTrue(long left, long right)
        {
            var first = new PropertyAmount(left);
            var second = new PropertyAmount(right);

            Assert.True(first < second);
        }

        [Theory]
        [InlineData(-1L)]
        [InlineData(0L)]
        [InlineData(1L)]
        public void LessThan_WithSame_ShouldReturnFalse(long value)
        {
            var first = new PropertyAmount(value);
            var second = new PropertyAmount(value);

            Assert.False(first < second);
        }

        [Theory]
        [InlineData(-1L, -2L)]
        [InlineData(0L, -1L)]
        [InlineData(1L, 0L)]
        public void LessThan_WithGreater_ShouldReturnFalse(long left, long right)
        {
            var first = new PropertyAmount(left);
            var second = new PropertyAmount(right);

            Assert.False(first < second);
        }

        [Theory]
        [InlineData(-2L, -1L)]
        [InlineData(-1L, 0L)]
        [InlineData(0L, 1L)]
        public void GreaterThan_WithLess_ShouldReturnFalse(long left, long right)
        {
            var first = new PropertyAmount(left);
            var second = new PropertyAmount(right);

            Assert.False(first > second);
        }

        [Theory]
        [InlineData(-1L)]
        [InlineData(0L)]
        [InlineData(1L)]
        public void GreaterThan_WithSame_ShouldReturnFalse(long value)
        {
            var first = new PropertyAmount(value);
            var second = new PropertyAmount(value);

            Assert.False(first > second);
        }

        [Theory]
        [InlineData(-1L, -2L)]
        [InlineData(0L, -1L)]
        [InlineData(1L, 0L)]
        public void GreaterThan_WithGreater_ShouldReturneTrue(long left, long right)
        {
            var first = new PropertyAmount(left);
            var second = new PropertyAmount(right);

            Assert.True(first > second);
        }

        [Theory]
        [InlineData(-2L, -1L)]
        [InlineData(-1L, 0L)]
        [InlineData(0L, 1L)]
        public void LessThanOrEqual_WithLess_ShouldReturnTrue(long left, long right)
        {
            var first = new PropertyAmount(left);
            var second = new PropertyAmount(right);

            Assert.True(first <= second);
        }

        [Theory]
        [InlineData(-1L)]
        [InlineData(0L)]
        [InlineData(1L)]
        public void LessThanOrEqual_WithSame_ShouldReturnTrue(long value)
        {
            var first = new PropertyAmount(value);
            var second = new PropertyAmount(value);

            Assert.True(first <= second);
        }

        [Theory]
        [InlineData(-1L, -2L)]
        [InlineData(0L, -1L)]
        [InlineData(1L, 0L)]
        public void LessThanOrEqual_WithGreater_ShouldReturnFalse(long left, long right)
        {
            var first = new PropertyAmount(left);
            var second = new PropertyAmount(right);

            Assert.False(first <= second);
        }

        [Theory]
        [InlineData(-2L, -1L)]
        [InlineData(-1L, 0L)]
        [InlineData(0L, 1L)]
        public void GreaterThanOrEqual_WithLess_ShouldReturnFalse(long left, long right)
        {
            var first = new PropertyAmount(left);
            var second = new PropertyAmount(right);

            Assert.False(first >= second);
        }

        [Theory]
        [InlineData(-1L)]
        [InlineData(0L)]
        [InlineData(1L)]
        public void GreaterThanOrEqual_WithSame_ShouldReturnTrue(long value)
        {
            var first = new PropertyAmount(value);
            var second = new PropertyAmount(value);

            Assert.True(first >= second);
        }

        [Theory]
        [InlineData(-1L, -2L)]
        [InlineData(0L, -1L)]
        [InlineData(1L, 0L)]
        public void GreaterThanOrEqual_WithGreater_ShouldReturnTrue(long left, long right)
        {
            var first = new PropertyAmount(left);
            var second = new PropertyAmount(right);

            Assert.True(first >= second);
        }

        [Fact]
        public void Negate_WithMinValue_ShouldThrow()
        {
            var v = new PropertyAmount(long.MinValue);
            Assert.Throws<OverflowException>(
                () => PropertyAmount.Negate(v)
            );
        }

        [Theory]
        [InlineData(long.MaxValue, -long.MaxValue)]
        [InlineData(1, -1)]
        [InlineData(0, 0)]
        [InlineData(-1, 1)]
        [InlineData(long.MinValue + 1, long.MaxValue)]
        public void Negate_WithNagatableAmount_ShouldReturnNagatedValue(long value, long expected)
        {
            // Arrange.
            var amount = new PropertyAmount(value);

            // Act.
            var negated = PropertyAmount.Negate(amount);

            // Assert.
            Assert.Equal(new PropertyAmount(expected), negated);
        }

        [Fact]
        public void NegateOperator_WithMinValue_ShouldThrow()
        {
            var v = new PropertyAmount(long.MinValue);
            Assert.Throws<OverflowException>(
                () => -v
            );
        }

        [Theory]
        [InlineData(long.MaxValue, -long.MaxValue)]
        [InlineData(1, -1)]
        [InlineData(0, 0)]
        [InlineData(-1, 1)]
        [InlineData(long.MinValue + 1, long.MaxValue)]
        public void NegateOperator_WithNagatableAmount_ShouldReturnNagatedValue(long value, long expected)
        {
            // Arrange.
            var amount = new PropertyAmount(value);

            // Act.
            var negated = -amount;

            // Assert.
            Assert.Equal(new PropertyAmount(expected), negated);
        }
    }
}
