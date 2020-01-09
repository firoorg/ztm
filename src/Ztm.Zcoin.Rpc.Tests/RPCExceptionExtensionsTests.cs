using NBitcoin.RPC;
using Xunit;

namespace Ztm.Zcoin.Rpc.Tests
{
    public sealed class RPCExceptionExtensionsTests
    {
        [Fact]
        public void IsInsufficientFee_WithNulError_ShouldReturnFasle()
        {
            var ex = RPCExceptionTesting.BuildException
            (
                new
                {
                    Result = "foo",
                    Error = (object)null
                },
                (RPCErrorCode)0,
                ""
            );

            Assert.False(ex.IsInsufficientFee());
        }

        [Fact]
        public void IsInsufficientFee_WithMismatchedStatus_ShouldReturnFalse()
        {
            var ex = RPCExceptionTesting.BuildException
            (
                new
                {
                    Result = (object)null,
                    Error = new
                    {
                        Code = -213,
                        Message = "Other error",
                    }
                },
                (RPCErrorCode)(-213),
                ""
            );

            Assert.False(ex.IsInsufficientFee());
        }

        [Fact]
        public void IsInsufficientFee_WithMatchedStatus_ShouldReturnTrue()
        {
            var ex = RPCExceptionTesting.BuildException
            (
                new
                {
                    Result = (object)null,
                    Error = new
                    {
                        Code = -212,
                        Message = "Error choosing inputs for the send transaction",
                    }
                },
                (RPCErrorCode)(-212),
                ""
            );

            Assert.True(ex.IsInsufficientFee());
        }

        [Fact]
        public void IsInsufficientToken_WithNullError_ShouldThrow()
        {
            var ex = RPCExceptionTesting.BuildException
            (
                new
                {
                    Result = "foo",
                    Error = (object)null
                },
                (RPCErrorCode)0,
                ""
            );

            Assert.False(ex.IsInsufficientToken());
        }

        [Fact]
        public void IsInsufficientToken_WithMissmatchedCode_ShouldReturnFalse()
        {
            var ex = RPCExceptionTesting.BuildException
            (
                new
                {
                    Result = (object)null,
                    Error = new
                    {
                        Code = RPCErrorCode.RPC_WALLET_ALREADY_UNLOCKED,
                        Message = "Sender has insufficient balance",
                    }
                },
                RPCErrorCode.RPC_WALLET_ALREADY_UNLOCKED,
                ""
            );

            Assert.False(ex.IsInsufficientToken());
        }

        [Fact]
        public void IsInsufficientToken_WithMissmatchedMessage_ShouldReturnFalse()
        {
            var ex = RPCExceptionTesting.BuildException
            (
                new
                {
                    Result = (object)null,
                    Error = new
                    {
                        Code = RPCErrorCode.RPC_TYPE_ERROR,
                        Message = "Another Error",
                    }
                },
                RPCErrorCode.RPC_TYPE_ERROR,
                ""
            );

            Assert.False(ex.IsInsufficientToken());
        }

        [Fact]
        public void IsInsufficientToken_WithMatchedCodeAndMessage_ShouldReturnTrue()
        {
            var ex = RPCExceptionTesting.BuildException
            (
                new
                {
                    Result = (object)null,
                    Error = new
                    {
                        Code = RPCErrorCode.RPC_TYPE_ERROR,
                        Message = "Sender has insufficient balance",
                    }
                },
                RPCErrorCode.RPC_TYPE_ERROR,
                ""
            );

            Assert.True(ex.IsInsufficientToken());
        }
    }
}