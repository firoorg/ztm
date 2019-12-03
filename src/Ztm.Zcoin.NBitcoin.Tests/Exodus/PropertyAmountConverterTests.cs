using System;
using Xunit;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.Zcoin.NBitcoin.Tests
{
    public sealed class PropertyAmountConverterTests
    {
        readonly PropertyAmountConverter subject;

        public PropertyAmountConverterTests()
        {
            this.subject = new PropertyAmountConverter();
        }

        [Theory]
        [InlineData(typeof(string))]
        [InlineData(typeof(int))]
        [InlineData(typeof(long))]
        [InlineData(typeof(decimal))]
        public void CanConvertFrom_WithSupportType_ShouldReturnTrue(Type type)
        {
            Assert.True(this.subject.CanConvertFrom(type));
        }

        [Theory]
        [InlineData(typeof(float))]
        [InlineData(typeof(double))]
        public void CanConvertFrom_WithUnsupportType_ShouldReturnFalse(Type type)
        {
            Assert.False(this.subject.CanConvertFrom(type));
        }

        [Theory]
        [InlineData(typeof(long))]
        [InlineData(typeof(decimal))]
        public void CanConvertTo_WithSupportType_ShouldReturnTrue(Type type)
        {
            Assert.True(this.subject.CanConvertTo(type));
        }

        [Theory]
        [InlineData(typeof(string))]
        [InlineData(typeof(int))]
        [InlineData(typeof(float))]
        [InlineData(typeof(double))]
        public void CanConvertTo_WithUnsupportType_ShouldReturnFalse(Type type)
        {
            Assert.False(this.subject.CanConvertTo(type));
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
        [InlineData("-9223372036854775808", -9223372036854775808L)]
        [InlineData("-1", -1L)]
        [InlineData("0", 0L)]
        [InlineData("9223372036854775807", 9223372036854775807L)]
        public void ConvertFrom_WithValidString_ShouldSuccess(string s, long expect)
        {
            var amount = (PropertyAmount)this.subject.ConvertFromString(s);

            Assert.Equal(expect, amount.Indivisible);
        }

        [Theory]
        [InlineData("")]
        [InlineData("a.0")]
        [InlineData("1a.1")]
        [InlineData("1.a")]
        [InlineData("1.1a")]
        [InlineData("0.000000001")]
        [InlineData("-92233720368.54775809")]
        [InlineData("92233720368.54775808")]
        [InlineData("aa")]
        [InlineData("1a")]
        [InlineData("-9223372036854775809")]
        [InlineData("9223372036854775808")]
        public void ConvertFrom_WithInvalidString_ShouldThrow(string s)
        {
            Assert.Throws<NotSupportedException>(() => this.subject.ConvertFromString(s));
        }

        [Theory]
        [InlineData(int.MinValue)]
        [InlineData(long.MinValue)]
        [InlineData(0)]
        [InlineData(0L)]
        [InlineData(int.MaxValue)]
        [InlineData(long.MaxValue)]
        public void ConvertFrom_WithInteger_ShouldSuccess(object value)
        {
            var amount = (PropertyAmount)this.subject.ConvertFrom(value);

            Assert.Equal(Convert.ToInt64(value), amount.Indivisible);
        }

        [Theory]
        [InlineData("-92233720368.54775808")]
        [InlineData("0.00000001")]
        [InlineData("0")]
        [InlineData("92233720368.54775807")]
        public void ConvertFrom_WithValidDecimal_ShouldSuccess(string s)
        {
            var value = decimal.Parse(s);
            var amount = (PropertyAmount)this.subject.ConvertFrom(value);

            Assert.Equal(value, amount.Divisible);
        }

        [Theory]
        [InlineData("0.000000009")]
        [InlineData("-92233720368.54775809")]
        [InlineData("92233720368.54775808")]
        public void ConvertFrom_WithInvalidDecimal_ShouldThrow(string s)
        {
            var value = decimal.Parse(s);

            Assert.Throws<NotSupportedException>(() => this.subject.ConvertFrom(value));
        }

        [Theory]
        [InlineData(0f)]
        [InlineData(0d)]
        public void ConvertFrom_WithUnsupportedType_ShouldThrow(object value)
        {
            Assert.Throws<NotSupportedException>(() => this.subject.ConvertFrom(value));
        }

        [Fact]
        public void ConvertTo_WithNullDestinationType_ShouldThrow()
        {
            var amount = new PropertyAmount(1);

            Assert.Throws<ArgumentNullException>(
                "destinationType",
                () => this.subject.ConvertTo(null, null, amount, null)
            );
        }

        [Fact]
        public void ConvertTo_WithTargetIsLong_ShouldSuccess()
        {
            var amount = new PropertyAmount(1);
            var converted = this.subject.ConvertTo(amount, typeof(long));

            Assert.Equal(amount.Indivisible, converted);
        }

        [Fact]
        public void ConvertTo_WithTargetIsDecimal_ShouldSuccess()
        {
            var amount = new PropertyAmount(1);
            var converted = this.subject.ConvertTo(amount, typeof(decimal));

            Assert.Equal(amount.Divisible, converted);
        }

        [Theory]
        [InlineData(typeof(string))]
        [InlineData(typeof(float))]
        [InlineData(typeof(double))]
        public void ConvertTo_WithUnsuppportedType_ShouldThrow(Type type)
        {
            var amount = new PropertyAmount(1);

            Assert.Throws<NotSupportedException>(() => this.subject.ConvertTo(amount, type));
        }
    }
}
