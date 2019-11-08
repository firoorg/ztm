using System;
using Xunit;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.Zcoin.NBitcoin.Tests.Exodus
{
    public sealed class TransactionFieldExceptionTests
    {
        [Fact]
        public void Constructor_WithNullField_ShouldThrow()
        {
            var msg = "qwerty";
            var inner = new Exception();

            Assert.Throws<ArgumentNullException>("field", () => new TransactionFieldException(null));
            Assert.Throws<ArgumentNullException>("field", () => new TransactionFieldException(null, msg));
            Assert.Throws<ArgumentNullException>("field", () => new TransactionFieldException(null, msg, inner));
        }

        [Fact]
        public void Constructor_WithNonNullField_FieldPropertyShouldHaveTheSameValue()
        {
            var field = "type";
            var ex = new TransactionFieldException(field);

            Assert.Equal(field, ex.Field);
        }

        [Fact]
        public void Constructor_WithMessage_MessagePropertyShouldHaveTheSameValue()
        {
            var field = "type";
            var msg = "qwerty";
            var ex = new TransactionFieldException(field, msg);

            Assert.Equal(field, ex.Field);
            Assert.Equal(msg, ex.Message);
        }

        [Fact]
        public void Constructor_WithInner_InnerExceptionPropertyShouldHaveTheSameValue()
        {
            var field = "type";
            var msg = "qwerty";
            var inner = new Exception();
            var ex = new TransactionFieldException(field, msg, inner);

            Assert.Equal(field, ex.Field);
            Assert.Equal(msg, ex.Message);
            Assert.Same(inner, ex.InnerException);
        }
    }
}
