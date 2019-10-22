using System;
using System.Net;
using Xunit;

namespace Ztm.WebApi.Tests
{
    public class CallbackTests
    {
        readonly Uri url;

        public CallbackTests()
        {
            this.url = new Uri("https://zcoin.io");
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
    }
}