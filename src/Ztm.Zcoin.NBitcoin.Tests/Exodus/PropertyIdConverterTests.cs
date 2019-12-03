using System;
using Xunit;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.Zcoin.NBitcoin.Tests.Exodus
{
    public sealed class PropertyIdConverterTests
    {
        readonly PropertyIdConverter subject;

        public PropertyIdConverterTests()
        {
            this.subject = new PropertyIdConverter();
        }

        [Theory]
        [InlineData(typeof(string))]
        [InlineData(typeof(int))]
        [InlineData(typeof(long))]
        public void CanConvertFrom_WithSupportedType_ShouldReturnTrue(Type type)
        {
            Assert.True(this.subject.CanConvertFrom(type));
        }

        [Theory]
        [InlineData(typeof(string))]
        [InlineData(typeof(long))]
        public void CanConvertTo_WithSupportedType_ShouldReturnTrue(Type type)
        {
            Assert.True(this.subject.CanConvertTo(type));
        }

        [Theory]
        [InlineData("1", 1L)]
        [InlineData("4294967295", 4294967295L)]
        [InlineData(1L, 1L)]
        [InlineData((long)uint.MaxValue, (long)uint.MaxValue)]
        public void ConvertFrom_WithValidInput_ShouldSuccess(object input, long expect)
        {
            var output = (PropertyId)this.subject.ConvertFrom(input);

            Assert.Equal(expect, output.Value);
        }

        [Theory]
        [InlineData("a")]
        [InlineData("1.0")]
        [InlineData("-9223372036854775809")]
        [InlineData("9223372036854775808")]
        [InlineData("-1")]
        [InlineData("0")]
        [InlineData("4294967296")]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(-1L)]
        [InlineData(0L)]
        [InlineData((long)uint.MaxValue + 1)]
        public void ConvertFrom_WithInvalidInput_ShouldThrow(object value)
        {
            Assert.Throws<NotSupportedException>(() => this.subject.ConvertFrom(value));
        }

        [Theory]
        [InlineData(1F)]
        [InlineData(1D)]
        public void ConvertFrom_WithUnsupportType_ShouldThrow(object value)
        {
            Assert.Throws<NotSupportedException>(() => this.subject.ConvertFrom(value));
        }

        [Fact]
        public void ConvertTo_WithNullDestinationType_ShouldThrow()
        {
            var id = new PropertyId(1);

            Assert.Throws<ArgumentNullException>("destinationType", () => this.subject.ConvertTo(null, null, id, null));
        }

        [Theory]
        [InlineData(1L, typeof(string), "1")]
        [InlineData(10000L, typeof(long), 10000L)]
        public void ConvertTo_WithSupportedType_ShouldSuccess(long id, Type type, object expected)
        {
            var converted = this.subject.ConvertTo(new PropertyId(id), type);

            Assert.Equal(expected, converted);
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(float))]
        [InlineData(typeof(double))]
        public void ConvertTo_WithUnsupportType_ShouldThrow(Type type)
        {
            var id = new PropertyId(1);

            Assert.Throws<NotSupportedException>(() => this.subject.ConvertTo(id, type));
        }
    }
}
