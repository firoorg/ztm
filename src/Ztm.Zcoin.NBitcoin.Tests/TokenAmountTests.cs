using System;
using Xunit;

namespace Ztm.Zcoin.NBitcoin.Tests
{
    public class TokenAmountTests
    {
        [Fact]
        public void IsValid_WithDefaultInstance_ShouldFalse()
        {
            Assert.False(default(TokenAmount).IsValid);
        }

        [Fact]
        public void Type_WithDefaultInstance_ShouldThrow()
        {
            Assert.Throws<InvalidOperationException>(() => default(TokenAmount).Type);
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

        [Theory]
        [InlineData("0.00000001")]
        [InlineData("1.00000000")]
        [InlineData("5.10000000")]
        [InlineData("92233720368.54775807")]
        public void Divisible_WithValidValue_ShouldSuccess(string value)
        {
            var amount = TokenAmount.Divisible(decimal.Parse(value));

            Assert.True(amount.IsValid);
            Assert.Equal(TokenType.Divisible, amount.Type);
            Assert.Equal(value, amount.ToString());
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

            Assert.True(amount.IsValid);
            Assert.Equal(TokenType.Indivisible, amount.Type);
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

        [Theory]
        [InlineData("0.00000001")]
        [InlineData("1.00000000")]
        [InlineData("5.10000000")]
        [InlineData("92233720368.54775807")]
        public void Parse_WithValidDivisibleValue_ShouldSuccess(string value)
        {
            var amount = TokenAmount.Parse(value);

            Assert.True(amount.IsValid);
            Assert.Equal(TokenType.Divisible, amount.Type);
            Assert.Equal(value, amount.ToString());
        }

        [Fact]
        public void Parse_WithIndivisibleValueExceedLimit_ShouldThrow()
        {
            Assert.Throws<FormatException>(() => TokenAmount.Parse("9223372036854775808"));
        }

        [Theory]
        [InlineData("1")]
        [InlineData("9223372036854775807")]
        public void Parse_WithValidIndivisibleValue_ShouldSuccess(string value)
        {
            var amount = TokenAmount.Parse(value);

            Assert.True(amount.IsValid);
            Assert.Equal(TokenType.Indivisible, amount.Type);
            Assert.Equal(value, amount.ToString());
        }

        [Fact]
        public void ToString_WithDefaultInstance_ShouldReturnEmptyString()
        {
            Assert.Empty(default(TokenAmount).ToString());
        }
    }
}
