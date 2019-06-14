using System;
using Xunit;

namespace Ztm.Zcoin.NBitcoin.Tests
{
    public class TokenIdTests
    {
        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData((long)uint.MaxValue + 1)]
        public void Constructor_WithInvalidValue_ShouldThrow(long value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                "value",
                () => new TokenId(value)
            );
        }

        [Fact]
        public void Constructor_WithValidValue_ShoulSuccess()
        {
            var id = new TokenId(1);

            Assert.True(id.IsValid);
            Assert.Equal(1, id.Value);
            Assert.Equal("1", id.ToString());
        }

        [Fact]
        public void IsValid_WithDefaultInstance_ShouldFalse()
        {
            Assert.False(default(TokenId).IsValid);
        }

        [Fact]
        public void Value_WithDefaultInstance_ShouldThrow()
        {
            Assert.Throws<InvalidOperationException>(() => default(TokenId).Value);
        }

        [Fact]
        public void Parse_WithNull_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>("s", () => TokenId.Parse(null));
        }

        [Theory]
        [InlineData("1")]
        [InlineData("4294967295")]
        public void Parse_WithValidString_ShouldSuccess(string value)
        {
            var id = TokenId.Parse(value);

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
            Assert.Throws<FormatException>(() => TokenId.Parse(value));
        }

        [Fact]
        public void ToString_WithDefaultInstance_ShouldReturnEmptyString()
        {
            var s = default(TokenId).ToString();

            Assert.Empty(s);
        }
    }
}
