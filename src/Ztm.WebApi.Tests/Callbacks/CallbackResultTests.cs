using System;
using Xunit;
using Ztm.WebApi.Callbacks;

namespace Ztm.WebApi.Tests.Callbacks
{
    public sealed class CallbackResultTests
    {
        readonly CallbackResult subject;

        public CallbackResultTests()
        {
            this.subject = new CallbackResult(CallbackResult.StatusSuccess, "Test data");
        }

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

        [Fact]
        public void Equals_WithOtherTypes_ShouldReturnFalse()
        {
            Assert.False(this.subject.Equals(string.Empty));

            Assert.False(this.subject.Equals(int.MaxValue));

            Assert.False(this.subject.Equals(long.MaxValue));

            Assert.False(this.subject.Equals(new object()));
        }

        [Fact]
        public void Equals_WithNull_ShouldReturnFalse()
        {
            Assert.False(this.subject.Equals(null));
        }

        [Fact]
        public void Equals_WithUnequalValue_ShouldReturnFalse()
        {
            CallbackResult other;

            other = new CallbackResult(CallbackResult.StatusError, this.subject.Data);
            Assert.False(this.subject.Equals(other));

            other = new CallbackResult(this.subject.Status, "");
            Assert.False(this.subject.Equals(other));
        }

        [Fact]
        public void Equals_WithSameValue_ShouldReturnTrue()
        {
            var other = new CallbackResult(this.subject.Status, this.subject.Data);

            Assert.True(this.subject.Equals(other));
        }

        [Fact]
        public void GetHashCode_WithSameValue_ShouldGetSameResult()
        {
            var other = new CallbackResult(this.subject.Status, this.subject.Data);

            Assert.Equal(this.subject.GetHashCode(), other.GetHashCode());
        }

        [Fact]
        public void GetHashCode_WithDifferenceValue_ShouldGetDifferenceResult()
        {
            CallbackResult other;

            other = new CallbackResult(CallbackResult.StatusError, this.subject.Data);

            Assert.NotEqual(this.subject.GetHashCode(), other.GetHashCode());

            other = new CallbackResult(this.subject.Status, "Difference data");

            Assert.NotEqual(this.subject.GetHashCode(), other.GetHashCode());
        }

        [Fact]
        public void GetHashCode_WithNullData_ShouldSuccess()
        {
            var other = new CallbackResult(CallbackResult.StatusSuccess, null);

            other.GetHashCode();
        }
    }
}