using System;
using Xunit;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.Zcoin.NBitcoin.Tests.Exodus
{
    public sealed class TransactionTooShortExceptionTests
    {
        [Theory]
        [InlineData(int.MinValue)]
        [InlineData(-1)]
        [InlineData(0)]
        public void Constructor_WithInvalidRequiredSize_ShouldThrow(int size)
        {
            var inner = new Exception();

            Assert.Throws<ArgumentOutOfRangeException>(
                "requiredSize",
                () => new TransactionTooShortException(size)
            );

            Assert.Throws<ArgumentOutOfRangeException>(
                "requiredSize",
                () => new TransactionTooShortException(size, inner)
            );
        }

        [Theory]
        [InlineData(1)]
        [InlineData(int.MaxValue)]
        public void Constructor_WithValidRequiredSize_RequiredSizePropertyShouldHaveTheSameValue(int size)
        {
            var ex = new TransactionTooShortException(size);

            Assert.Equal(size, ex.RequiredSize);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(int.MaxValue)]
        public void Constructor_WithInner_InnerExceptionPropertyShouldHaveTheSameValue(int size)
        {
            var inner = new Exception();
            var ex = new TransactionTooShortException(size, inner);

            Assert.Equal(size, ex.RequiredSize);
            Assert.NotEmpty(ex.Message);
            Assert.Same(inner,ex.InnerException);
        }
    }
}
