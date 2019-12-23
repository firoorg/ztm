using System;
using System.Net;
using Xunit;
using Ztm.WebApi.Callbacks;

namespace Ztm.WebApi.Tests.Callbacks
{
    public class CallbackTests
    {
        readonly Uri url;
        readonly Callback subject;

        public CallbackTests()
        {
            this.url = new Uri("https://zcoin.io");
            this.subject = new Callback(Guid.NewGuid(), IPAddress.Loopback, DateTime.UtcNow, false, url);
        }

        [Fact]
        public void ConstructCallback_WithNullRegisteredIP_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "registeredIp",
                () => new Callback(Guid.Empty, null, DateTime.Now, false, this.url)
            );
        }

        [Fact]
        public void ConstructCallback_WithNullUrl_ShouldThow()
        {
            Assert.Throws<ArgumentNullException>(
                "url",
                () => new Callback(Guid.Empty, IPAddress.Loopback, DateTime.Now, false, null)
            );
        }

        [Fact]
        public void ConstructCallback_WithValueArgs_ShouldSuccess()
        {
            new Callback(Guid.Empty, IPAddress.Loopback, DateTime.Now, false, this.url);
        }

        [Fact]
        public void Compare_WithNull_ShouldGreater()
        {
            var result = this.subject.CompareTo(null);

            Assert.True(result > 0);
        }

        [Fact]
        public void Equals_WithNull_ShouldReturnFalse()
        {
            Assert.False(this.subject.Equals(null));
        }

        [Fact]
        public void Equals_WithOtherType_ShouldReturnFalse()
        {
            Assert.False(this.subject.Equals(""));
        }

        [Fact]
        public void Equals_WithDifferenceId_ShouldReturnFalse()
        {
            Callback other;

            other = new Callback(Guid.NewGuid(), this.subject.RegisteredIp, this.subject.RegisteredTime, false, this.subject.Url);

            Assert.False(this.subject.Equals(other));
        }

        [Fact]
        public void Equals_WithEqualValue_ShouldReturnTrue()
        {
            Callback other;

            other = new Callback(this.subject.Id, this.subject.RegisteredIp, this.subject.RegisteredTime, false, this.subject.Url);

            Assert.True(this.subject.Equals(other));
        }
    }
}