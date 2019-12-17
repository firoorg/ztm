using System;
using Xunit;
using Ztm.WebApi.Callbacks;

namespace Ztm.WebApi.Tests.Callbacks
{
    public sealed class CallbackResultTests
    {
        [Fact]
        public void Construct_WithNullStatus_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "status",
                () => new CallbackResult(null, null)
            );
        }

        [Fact]
        public void Construct_WithNullData_ShouldSuccess()
        {
            // Arrange.
            var status = CallbackResult.StatusError;

            // Act.
            var r = new CallbackResult(status, null);

            // Assert.
            Assert.Equal(status, r.Status);
            Assert.Null(r.Data);
        }

        [Fact]
        public void Construct_WithNonEmptyStatusAndData_ShouldSuccess()
        {
            // Arrange.
            var status = CallbackResult.StatusError;
            var data = new object();

            // Act.
            var r = new CallbackResult(status, data);

            // Assert.
            Assert.Equal(status, r.Status);
            Assert.Same(data, r.Data);
        }
    }
}