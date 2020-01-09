using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using Ztm.WebApi.Callbacks;
using Ztm.WebApi.Controllers;

namespace Ztm.WebApi.Tests.Controllers
{
    public sealed class ControllerHelperTests
    {
        readonly Mock<ICallbackRepository> callbackRepository;

        readonly ControllerHelper subject;

        public ControllerHelperTests()
        {
            this.callbackRepository = new Mock<ICallbackRepository>();

            this.subject = new ControllerHelper(this.callbackRepository.Object);
        }

        [Fact]
        public void Construct_WithNullArgument_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "callbackRepository",
                () => new ControllerHelper(null)
            );
        }

        [Fact]
        public async Task RegisterCallbackAsync_WithNullArgument_ShouldThrow()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                "controller",
                () => this.subject.RegisterCallbackAsync(null, CancellationToken.None)
            );
        }

        [Fact]
        public async Task RegisterCallbackAsync_WithoutCallbackUrl_ShouldReturnNull()
        {
            // Arrange.
            var controller = new Mock<ControllerBase>();
            controller.Object.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act.
            var callback = await this.subject.RegisterCallbackAsync(controller.Object, CancellationToken.None);

            // Assert.
            this.callbackRepository.Verify
            (
                r => r.AddAsync
                (
                    It.IsAny<IPAddress>(),
                    It.IsAny<Uri>(),
                    It.IsAny<CancellationToken>()
                ),
                Times.Never()
            );

            Assert.Null(callback);
        }

        [Fact]
        public async Task RegisterCallbackAsync_WithCallbackUrl_ShouldSuccess()
        {
            // Arrange.
            var callerIP = IPAddress.Loopback;
            var rawUrl = "https://zcoin.io/callback";
            var url = new Uri(rawUrl);

            var controller = new Mock<ControllerBase>();

            var httpContext = new DefaultHttpContext();
            httpContext.Connection.RemoteIpAddress = callerIP;
            httpContext.Request.Headers.TryAdd("X-Callback-URL", rawUrl);

            controller.Object.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Callback Setup.
            var callback = new Callback
            (
                Guid.NewGuid(),
                callerIP,
                DateTime.UtcNow,
                false,
                url
            );

            this.callbackRepository.Setup(
                r => r.AddAsync
                (
                    callerIP,
                    url,
                    It.IsAny<CancellationToken>()
                )).ReturnsAsync(callback).Verifiable();

            // Act.
            var returnedCallback = await this.subject.RegisterCallbackAsync(controller.Object, CancellationToken.None);

            // Assert.
            this.callbackRepository.Verify();

            Assert.NotNull(returnedCallback);
            Assert.Equal(callback, returnedCallback);
        }
    }
}