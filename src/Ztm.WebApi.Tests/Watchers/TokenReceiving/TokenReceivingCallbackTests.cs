using System;
using System.Net;
using Xunit;
using Ztm.Testing;
using Ztm.WebApi.Callbacks;
using Ztm.WebApi.Watchers.TokenReceiving;

namespace Ztm.WebApi.Tests.Watchers.TokenReceiving
{
    public sealed class TokenReceivingCallbackTests
    {
        readonly Callback callback;
        readonly TokenReceivingCallback subject;

        public TokenReceivingCallbackTests()
        {
            this.callback = new Callback(
                Guid.NewGuid(),
                IPAddress.Parse("192.168.1.2"),
                DateTime.Now,
                true,
                new Uri("http://localhost/a"));

            this.subject = new TokenReceivingCallback(this.callback, "timeout");
        }

        [Fact]
        public void Constructor_WithNullCallback_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>("callback", () => new TokenReceivingCallback(null, "timeout"));
        }

        [Fact]
        public void Constructor_WithNullTimeoutStatus_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "timeoutStatus",
                () => new TokenReceivingCallback(this.callback, null));
        }

        [Fact]
        public void Constructor_WhenSucceeded_ShouldInitializeProperties()
        {
            Assert.Equal(this.callback, this.subject.Callback);
            Assert.Equal(this.callback.Completed, this.subject.Completed);
            Assert.Equal("timeout", this.subject.TimeoutStatus);
        }

        [Fact]
        public void Equals_WithEqual_ShouldReturnTrue()
        {
            var results = EqualityTesting.TestEquals(
                this.subject,
                s => new TokenReceivingCallback(s.Callback, s.TimeoutStatus));

            Assert.DoesNotContain(false, results);
        }

        [Fact]
        public void Equals_WithUnequal_ShouldReturnFalse()
        {
            var results = EqualityTesting.TestInequal(
                this.subject,
                s => new TokenReceivingCallback(
                    new Callback(
                        Guid.NewGuid(),
                        IPAddress.Parse("192.168.1.3"),
                        DateTime.Now,
                        false,
                        new Uri("http://localhost/b")),
                    s.TimeoutStatus),
                s => new TokenReceivingCallback(s.Callback, ""));

            Assert.DoesNotContain(true, results);
        }
    }
}
