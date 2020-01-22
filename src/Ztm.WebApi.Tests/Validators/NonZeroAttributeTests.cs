using Xunit;
using Ztm.WebApi.Validators;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.WebApi.Tests.Validators
{
    public sealed class NonZeroAttributeTests
    {
        readonly NonZeroAttribute subject;

        public NonZeroAttributeTests()
        {
            this.subject = new NonZeroAttribute();
        }

        [Fact]
        public void IsValid_WithNull_ShouldReturnTrue()
        {
            var result = this.subject.IsValid(null);

            Assert.True(result);
        }

        [Fact]
        public void IsValid_WithZeroPropertyAmount_ShouldReturnFalse()
        {
            var result = this.subject.IsValid(PropertyAmount.Zero);

            Assert.False(result);
        }

        [Theory]
        [InlineData(long.MaxValue)]
        [InlineData(long.MinValue)]
        [InlineData(-1L)]
        public void IsValid_WithNonZeroPropertyAmount_ShouldReturnTrue(long amount)
        {
            var result = this.subject.IsValid(new PropertyAmount(amount));

            Assert.True(result);
        }

        [Theory]
        [InlineData(1f)]
        [InlineData(1d)]
        [InlineData((byte)1)]
        [InlineData((short)1)]
        [InlineData(1)]
        [InlineData(1L)]
        [InlineData("1")]
        public void IsValid_WithUnsupportedType_ShouldReturnFalse(object value)
        {
            var result = this.subject.IsValid(value);

            Assert.False(result);
        }
    }
}
