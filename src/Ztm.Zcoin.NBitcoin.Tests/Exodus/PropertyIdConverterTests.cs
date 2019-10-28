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
        [InlineData("1", "1")]
        [InlineData("4294967295", "4294967295")]
        [InlineData(1, "1")]
        [InlineData(2147483647, "2147483647")]
        [InlineData(1L, "1")]
        [InlineData(4294967295L, "4294967295")]
        public void ConvertFrom_WithValidValue_ShouldSuccess(object value, string expected)
        {
            var id = (PropertyId)this.subject.ConvertFrom(value);

            Assert.True(id.IsValid);
            Assert.Equal(expected, id.ToString());
        }

        [Theory]
        [InlineData("-9223372036854775809")]
        [InlineData("-9223372036854775808")]
        [InlineData("0")]
        [InlineData("4294967296")]
        [InlineData("9223372036854775807")]
        [InlineData("9223372036854775808")]
        [InlineData(-2147483648)]
        [InlineData(0)]
        [InlineData(-9223372036854775808L)]
        [InlineData(-2147483648L)]
        [InlineData(0L)]
        [InlineData(4294967296L)]
        [InlineData(9223372036854775807)]
        public void ConvertFrom_WithInvalidValue_ShouldThrow(object value)
        {
            Assert.Throws<NotSupportedException>(() => this.subject.ConvertFrom(value));
        }

        [Fact]
        public void ConvertTo_WithNullDestinationType_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "destinationType",
                () => this.subject.ConvertTo(null, null, new PropertyId(1), null)
            );
        }

        [Fact]
        public void ConvertTo_WithInvalidValue_ShouldThrow()
        {
            Assert.Throws<NotSupportedException>(() => this.subject.ConvertTo(default(PropertyId), typeof(string)));
        }

        [Theory]
        [InlineData(1L, typeof(string), "1")]
        [InlineData(10000L, typeof(long), 10000L)]
        public void ConvertTo_WithSupportedType_ShouldSuccess(long id, Type type, object expected)
        {
            var converted = this.subject.ConvertTo(new PropertyId(id), type);

            Assert.Equal(expected, converted);
        }
    }
}
