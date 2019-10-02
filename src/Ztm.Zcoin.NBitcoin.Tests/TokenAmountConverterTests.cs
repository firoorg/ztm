using System;
using Xunit;

namespace Ztm.Zcoin.NBitcoin.Tests
{
    public class TokenAmountConverterTests
    {
        readonly TokenAmountConverter subject;

        public TokenAmountConverterTests()
        {
            this.subject = new TokenAmountConverter();
        }

        [Fact]
        public void CanConvertFrom_WithStringType_ShouldReturnTrue()
        {
            Assert.True(this.subject.CanConvertFrom(typeof(string)));
        }

        [Fact]
        public void CanConvertFrom_WithIntType_ShouldReturnTrue()
        {
            Assert.True(this.subject.CanConvertFrom(typeof(int)));
        }

        [Fact]
        public void CanConvertFrom_WithLongType_ShouldReturnTrue()
        {
            Assert.True(this.subject.CanConvertFrom(typeof(long)));
        }

        [Fact]
        public void CanConvertFrom_WithFloatType_ShouldReturnTrue()
        {
            Assert.True(this.subject.CanConvertFrom(typeof(float)));
        }

        [Fact]
        public void CanConvertFrom_WithDoubleType_ShouldReturnTrue()
        {
            Assert.True(this.subject.CanConvertFrom(typeof(double)));
        }

        [Fact]
        public void CanConvertFrom_WithDecimalType_ShouldReturnTrue()
        {
            Assert.True(this.subject.CanConvertFrom(typeof(decimal)));
        }

        [Fact]
        public void CanConvertTo_WithStringType_ShouldReturnTrue()
        {
            Assert.True(this.subject.CanConvertTo(typeof(string)));
        }

        [Theory]
        [InlineData("0.00000001", TokenType.Divisible)]
        [InlineData("10000.10000000", TokenType.Divisible)]
        [InlineData("9999.99999999", TokenType.Divisible)]
        [InlineData("5000", TokenType.Indivisible)]
        public void ConvertFrom_WithValidString_ShouldSuccess(string value, TokenType type)
        {
            var amount = (TokenAmount)this.subject.ConvertFromString(value);

            Assert.True(amount.IsValid);
            Assert.Equal(type, amount.Type);
            Assert.Equal(value, amount.ToString());
        }

        [Theory]
        [InlineData("")]
        [InlineData("-0.00000001")]
        [InlineData("0.000000001")]
        [InlineData("0.000000011")]
        [InlineData("92233720368.54775808")]
        [InlineData("-1")]
        [InlineData("9223372036854775808")]
        public void ConvertFrom_WithInvalidString_ShouldThrow(string value)
        {
            Assert.Throws<NotSupportedException>(() => this.subject.ConvertFromString(value));
        }

        [Theory]
        [InlineData("0.00000000")]
        [InlineData("0")]
        public void ConvertFrom_WithStringZero_ShouldReturnInvalidAmount(string value)
        {
            var amount = (TokenAmount)this.subject.ConvertFromString(value);

            Assert.False(amount.IsValid);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(1L)]
        [InlineData(int.MaxValue)]
        [InlineData(long.MaxValue)]
        public void ConvertFrom_WithValidInteger_ShouldSuccess(object value)
        {
            var amount = (TokenAmount)this.subject.ConvertFrom(value);
            var expected = TokenAmount.Indivisible(value is int ? (int)value : (long)value);

            Assert.True(amount.IsValid);
            Assert.Equal(TokenType.Indivisible, amount.Type);
            Assert.Equal(expected.ToString(), amount.ToString());
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-1L)]
        [InlineData(0)]
        [InlineData(0L)]
        public void ConvertFrom_WithInvalidInteger_ShouldThrow(object value)
        {
            Assert.Throws<NotSupportedException>(() => this.subject.ConvertFrom(value));
        }

        [Theory]
        [InlineData(0.00000001f)]
        [InlineData(0.00000001d)]
        [InlineData(10000.99999999f)]
        [InlineData(10000.99999999d)]
        public void ConvertFrom_WithValidFloatingPoint_ShouldSuccess(object value)
        {
            var amount = (TokenAmount)this.subject.ConvertFrom(value);
            var expected = TokenAmount.Divisible(value is float ? (decimal)(float)value : (decimal)(double)value);

            Assert.True(amount.IsValid);
            Assert.Equal(TokenType.Divisible, amount.Type);
            Assert.Equal(expected.ToString(), amount.ToString());
        }

        [Theory]
        [InlineData(-0.1f)]
        [InlineData(-0.1d)]
        [InlineData(0f)]
        [InlineData(0d)]
        [InlineData(0.000000001f)]
        [InlineData(0.000000001d)]
        [InlineData(92233720368.54775808d)]
        public void ConvertFrom_WithInvalidFloatingPoint_ShouldThrow(object value)
        {
            Assert.Throws<NotSupportedException>(() => this.subject.ConvertFrom(value));
        }

        [Theory]
        [InlineData("0.00000001")]
        [InlineData("92233720368.54775807")]
        public void ConvertFrom_WithValidDecimal_ShouldSuccess(string value)
        {
            var amount = (TokenAmount)this.subject.ConvertFrom(decimal.Parse(value));

            Assert.True(amount.IsValid);
            Assert.Equal(TokenType.Divisible, amount.Type);
            Assert.Equal(value, amount.ToString());
        }

        [Theory]
        [InlineData("-0.1")]
        [InlineData("0")]
        [InlineData("0.000000001")]
        [InlineData("92233720368.54775808")]
        public void ConvertFrom_WithInvalidDecimal_ShouldThrow(string value)
        {
            Assert.Throws<NotSupportedException>(() => this.subject.ConvertFrom(decimal.Parse(value)));
        }

        [Fact]
        public void ConvertTo_WithDefaultInstance_ShouldThrow()
        {
            Assert.Throws<NotSupportedException>(() => this.subject.ConvertToString(default(TokenAmount)));
        }

        [Fact]
        public void ConvertTo_WithValidValueAndTargetIsString_ShouldSuccess()
        {
            var converted = this.subject.ConvertToString(TokenAmount.Divisible(1));

            Assert.Equal("1.00000000", converted);
        }

        [Fact]
        public void GetStandardValues_WithDefault_ShouldReturnValidValues()
        {
            Assert.True(this.subject.GetStandardValuesSupported());

            var values = this.subject.GetStandardValues();

            foreach (TokenAmount amount in values)
            {
                Assert.True(amount.IsValid);
            }
        }
    }
}
