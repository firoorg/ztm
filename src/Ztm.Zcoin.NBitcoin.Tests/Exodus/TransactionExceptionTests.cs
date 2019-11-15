using System;
using Xunit;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.Zcoin.NBitcoin.Tests.Exodus
{
    public sealed class TransactionExceptionTests
    {
        [Fact]
        public void Constructor_WithMessage_MessagePropertyShouldHaveTheSameValue()
        {
            var msg = "qwerty";
            var ex = new TransactionException(msg);

            Assert.Equal(msg, ex.Message);
        }

        [Fact]
        public void Constructor_WithInner_InnerExceptionPropertyShouldHaveTheSameValue()
        {
            var msg = "qwerty";
            var inner = new Exception();
            var ex = new TransactionException(msg, inner);

            Assert.Equal(msg, ex.Message);
            Assert.Same(inner, ex.InnerException);
        }
    }
}
