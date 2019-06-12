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
            Assert.Equal(1U, id.Value);
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
    }
}
