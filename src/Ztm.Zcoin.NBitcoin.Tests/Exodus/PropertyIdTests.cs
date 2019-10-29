using System;
using Xunit;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.Zcoin.NBitcoin.Tests.Exodus
{
    public sealed class PropertyIdTests
    {
        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData((long)uint.MaxValue + 1)]
        public void Constructor_WithInvalidValue_ShouldThrow(long value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                "value",
                () => new PropertyId(value)
            );
        }

        [Fact]
        public void Constructor_WithValidValue_ShoulSuccess()
        {
            var id = new PropertyId(1);

            Assert.True(id.IsValid);
            Assert.Equal(1, id.Value);
            Assert.Equal("1", id.ToString());
        }

        [Fact]
        public void IsValid_WithDefaultInstance_ShouldFalse()
        {
            Assert.False(default(PropertyId).IsValid);
        }

        [Fact]
        public void Value_WithDefaultInstance_ShouldThrow()
        {
            Assert.Throws<InvalidOperationException>(() => default(PropertyId).Value);
        }

        [Fact]
        public void Parse_WithNull_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>("s", () => PropertyId.Parse(null));
        }

        [Theory]
        [InlineData("1")]
        [InlineData("4294967295")]
        public void Parse_WithValidString_ShouldSuccess(string value)
        {
            var id = PropertyId.Parse(value);

            Assert.True(id.IsValid);
            Assert.Equal(value, id.ToString());
        }

        [Theory]
        [InlineData("-9223372036854775809")]
        [InlineData("-9223372036854775808")]
        [InlineData("0")]
        [InlineData("4294967296")]
        [InlineData("9223372036854775808")]
        public void Parse_WithInvalidString_ShouldThrow(string value)
        {
            Assert.Throws<FormatException>(() => PropertyId.Parse(value));
        }

        [Fact]
        public void ToString_WithDefaultInstance_ShouldReturnEmptyString()
        {
            var s = default(PropertyId).ToString();

            Assert.Empty(s);
        }
    }
}
