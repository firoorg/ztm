using System;
using Xunit;

namespace Ztm.Zcoin.NBitcoin.Tests
{
    public class TokenAmountTests
    {
        [Fact]
        public void IsValid_WithDefaultValue_ShouldFalse()
        {
            Assert.False(default(TokenAmount).IsValid);
        }

        [Fact]
        public void Value_WithDefaultValue_ShouldThrow()
        {
            Assert.Throws<InvalidOperationException>(() => default(TokenAmount).Value);
        }

        [Fact]
        public void Divisible_WithNegativeValue_ShoudThrow()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                "value",
                () => TokenAmount.Divisible(-1)
            );
        }

        [Fact]
        public void Divisible_WithZeroValue_ShoudThrow()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                "value",
                () => TokenAmount.Divisible(0)
            );
        }

        [Fact]
        public void Divisible_WithExceedLimit_ShouldThrow()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                "value",
                () => TokenAmount.Divisible(92233720368.54775808m)
            );
        }

        [Fact]
        public void Divisible_WithTooMuchPrecision_ShouldThrow()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                "value",
                () => TokenAmount.Divisible(0.000000001m)
            );
        }

        [Fact]
        public void Divisible_WithValidFractional_ShouldSuccess()
        {
            var amount = TokenAmount.Divisible(0.00000001m);

            Assert.True(amount.IsDivisible);
            Assert.False(amount.IsIndivisible);
            Assert.True(amount.IsValid);
            Assert.Equal(0.00000001m, amount.Value);
            Assert.Equal("0.00000001", amount.ToString());
        }

        [Fact]
        public void Divisible_WithNonFractional_ShouldSuccess()
        {
            var amount = TokenAmount.Divisible(10000);

            Assert.True(amount.IsDivisible);
            Assert.False(amount.IsIndivisible);
            Assert.True(amount.IsValid);
            Assert.Equal(10000, amount.Value);
            Assert.Equal("10000.00000000", amount.ToString());
        }

        [Fact]
        public void Indivisible_WithNegativeValue_ShouldThrow()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                "value",
                () => TokenAmount.Indivisible(-1)
            );
        }

        [Fact]
        public void Indivisible_WithZeroValue_ShouldThrow()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                "value",
                () => TokenAmount.Indivisible(0)
            );
        }

        [Fact]
        public void Indivisible_WithPositiveValue_ShouldSuccess()
        {
            var amount = TokenAmount.Indivisible(90000);

            Assert.False(amount.IsDivisible);
            Assert.True(amount.IsIndivisible);
            Assert.True(amount.IsValid);
            Assert.Equal(90000, amount.Value);
            Assert.Equal("90000", amount.ToString());
        }

        [Fact]
        public void Parse_WithNull_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "s",
                () => TokenAmount.Parse(null)
            );
        }

        [Fact]
        public void Parse_WithNegative_ShouldThrow()
        {
            Assert.Throws<FormatException>(() => TokenAmount.Parse("-1"));
        }

        [Fact]
        public void Parse_WithZero_ShouldReturnInvalid()
        {
            var amount = TokenAmount.Parse("0");

            Assert.False(amount.IsValid);
        }

        [Fact]
        public void Parse_WithDivisibleValueExceedLimit_ShouldThrow()
        {
            Assert.Throws<FormatException>(() => TokenAmount.Parse("92233720368.54775808"));
        }

        [Fact]
        public void Parse_WithDivisibleValueTooMuchPrecision_ShouldThrow()
        {
            Assert.Throws<FormatException>(() => TokenAmount.Parse("0.000000001"));
        }

        [Fact]
        public void Parse_WithValidDivisibleValue_ShouldSuccess()
        {
            var amount = TokenAmount.Parse("100000.1");

            Assert.True(amount.IsDivisible);
            Assert.False(amount.IsIndivisible);
            Assert.True(amount.IsValid);
            Assert.Equal(100000.1m, amount.Value);
            Assert.Equal("100000.10000000", amount.ToString());
        }

        [Fact]
        public void Parse_WithIndivisibleValueExceedLimit_ShouldThrow()
        {
            Assert.Throws<FormatException>(() => TokenAmount.Parse("9223372036854775808"));
        }

        [Fact]
        public void Parse_WithValidIndivisibleValue_ShouldSuccess()
        {
            var amount = TokenAmount.Parse("10000");

            Assert.False(amount.IsDivisible);
            Assert.True(amount.IsIndivisible);
            Assert.True(amount.IsValid);
            Assert.Equal(10000, amount.Value);
            Assert.Equal("10000", amount.ToString());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Satoshi_WithNegativeValue_ShouldThrow(bool divisible)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                "value",
                () => TokenAmount.Satoshi(-1, divisible)
            );
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Satoshi_WithZeroValue_ShouldThrow(bool divisible)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                "value",
                () => TokenAmount.Satoshi(0, divisible)
            );
        }

        [Fact]
        public void Satoshi_WithFractionalValueButNotDivisible_ShouldThrow()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                "value",
                () => TokenAmount.Satoshi(99999999, false)
            );
        }

        [Fact]
        public void Satoshi_WithValidDivisibleValue_ShouldSuccess()
        {
            var amount = TokenAmount.Satoshi(300000000000, true);

            Assert.True(amount.IsDivisible);
            Assert.False(amount.IsIndivisible);
            Assert.True(amount.IsValid);
            Assert.Equal(3000.00000000m, amount.Value);
            Assert.Equal("3000.00000000", amount.ToString());
        }

        [Fact]
        public void Satoshi_WithValidIndivisibleValue_ShouldSuccess()
        {
            var amount = TokenAmount.Satoshi(300000000000, false);

            Assert.False(amount.IsDivisible);
            Assert.True(amount.IsIndivisible);
            Assert.True(amount.IsValid);
            Assert.Equal(3000, amount.Value);
            Assert.Equal("3000", amount.ToString());
        }

        [Fact]
        public void ToString_WithDefaultValue_ShouldReturnEmptyString()
        {
            Assert.Empty(default(TokenAmount).ToString());
        }
    }
}
