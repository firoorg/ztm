using Xunit;
using Ztm.WebApi.Validators;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.WebApi.Tests.Validators
{
    public sealed class PositiveAttributeTests
    {
        readonly PositiveAttribute subject;

        public PositiveAttributeTests()
        {
            this.subject = new PositiveAttribute();
        }

        [Fact]
        public void IsValid_WithNull_ShouldReturnTrue()
        {
            var result = this.subject.IsValid(null);

            Assert.True(result);
        }

        [Theory]
        [InlineData(long.MinValue)]
        [InlineData(-1)]
        public void IsValid_WithNegativePropertyAmount_ShouldReturnFalse(long amount)
        {
            var result = this.subject.IsValid(new PropertyAmount(amount));

            Assert.False(result);
        }

        [Fact]
        public void IsValid_WithZeroPropertyAmount_ShouldReturnTrue()
        {
            var result = this.subject.IsValid(PropertyAmount.Zero);

            Assert.True(result);
        }

        [Theory]
        [InlineData(1L)]
        [InlineData(long.MaxValue)]
        public void IsValid_WithPositivePropertyAmount_ShouldReturnTrue(long amount)
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
