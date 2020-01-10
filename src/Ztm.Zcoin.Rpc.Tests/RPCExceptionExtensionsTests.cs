using NBitcoin.RPC;
using Xunit;
using Ztm.Zcoin.Testing;

namespace Ztm.Zcoin.Rpc.Tests
{
    public sealed class RPCExceptionExtensionsTests
    {
        [Fact]
        public void IsInsufficientFee_WithNulError_ShouldReturnFasle()
        {
            var ex = RPCExceptionTesting.BuildException((RPCErrorCode)0, "", new
            {
                Result = "foo",
                Error = (object)null
            });

            Assert.False(ex.IsInsufficientFee());
        }

        [Fact]
        public void IsInsufficientFee_WithMismatchedStatus_ShouldReturnFalse()
        {
            var ex = RPCExceptionTesting.BuildException((RPCErrorCode)(-213), "", new
            {
                Result = (object)null,
                Error = new
                {
                    Code = -213,
                    Message = "Other error",
                }
            });

            Assert.False(ex.IsInsufficientFee());
        }

        [Fact]
        public void IsInsufficientFee_WithMatchedStatus_ShouldReturnTrue()
        {
            var ex = RPCExceptionTesting.BuildException((RPCErrorCode)(-212), "", new
            {
                Result = (object)null,
                Error = new
                {
                    Code = -212,
                    Message = "Error choosing inputs for the send transaction",
                }
            });

            Assert.True(ex.IsInsufficientFee());
        }

        [Fact]
        public void IsInsufficientToken_WithNullError_ShouldThrow()
        {
            var ex = RPCExceptionTesting.BuildException((RPCErrorCode)0, "", new
            {
                Result = "foo",
                Error = (object)null
            });

            Assert.False(ex.IsInsufficientToken());
        }

        [Fact]
        public void IsInsufficientToken_WithMissmatchedCode_ShouldReturnFalse()
        {
            var ex = RPCExceptionTesting.BuildException(RPCErrorCode.RPC_WALLET_ALREADY_UNLOCKED, "", new
            {
                Result = (object)null,
                Error = new
                {
                    Code = RPCErrorCode.RPC_WALLET_ALREADY_UNLOCKED,
                    Message = "Sender has insufficient balance",
                }
            });

            Assert.False(ex.IsInsufficientToken());
        }

        [Fact]
        public void IsInsufficientToken_WithMissmatchedMessage_ShouldReturnFalse()
        {
            var ex = RPCExceptionTesting.BuildException(RPCErrorCode.RPC_TYPE_ERROR, "", new
            {
                Result = (object)null,
                Error = new
                {
                    Code = RPCErrorCode.RPC_TYPE_ERROR,
                    Message = "Another Error",
                }
            });

            Assert.False(ex.IsInsufficientToken());
        }

        [Fact]
        public void IsInsufficientToken_WithMatchedCodeAndMessage_ShouldReturnTrue()
        {
            var ex = RPCExceptionTesting.BuildException(RPCErrorCode.RPC_TYPE_ERROR, "", new
            {
                Result = (object)null,
                Error = new
                {
                    Code = RPCErrorCode.RPC_TYPE_ERROR,
                    Message = "Sender has insufficient balance",
                }
            });

            Assert.True(ex.IsInsufficientToken());
        }
    }
}