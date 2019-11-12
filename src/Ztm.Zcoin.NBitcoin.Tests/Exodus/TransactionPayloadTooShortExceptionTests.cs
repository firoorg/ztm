using System;
using Xunit;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.Zcoin.NBitcoin.Tests.Exodus
{
    public sealed class TransactionPayloadTooShortExceptionTests
    {
        [Fact]
        public void Constructor_WithRequiredSize_RequiredSizePropertyShouldAdditionWithMinimumTransactionSize()
        {
            var ex = new TransactionPayloadTooShortException(4);

            Assert.Equal(TransactionEncoder.MinSize + 4, ex.RequiredSize);
        }

        [Fact]
        public void Constructor_WithInner_InnerExceptionPropertyShouldHaveTheSameValue()
        {
            var inner = new Exception();
            var ex = new TransactionPayloadTooShortException(0, inner);

            Assert.Equal(TransactionEncoder.MinSize, ex.RequiredSize);
            Assert.Same(inner, ex.InnerException);
        }
    }
}
