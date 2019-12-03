using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Internal;
using Moq;
using Xunit;

namespace Ztm.Hosting.AspNetCore.Tests
{
    public sealed class BackgroundServiceExceptionHandlerTests
    {
        readonly Mock<ILogger<BackgroundServiceErrorLogger>> logger;
        readonly BackgroundServiceExceptionHandler subject;

        public BackgroundServiceExceptionHandlerTests()
        {
            this.logger = new Mock<ILogger<BackgroundServiceErrorLogger>>();

            var factory = new Mock<ILoggerFactory>();
            factory.Setup(f => f.CreateLogger(typeof(BackgroundServiceErrorLogger).FullName))
                   .Returns(this.logger.Object);

            this.subject = new BackgroundServiceExceptionHandler(factory.Object);
        }

        [Fact]
        public void Constructor_WithNullLoggerFactory_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>("loggerFactory", () => new BackgroundServiceExceptionHandler(null));
        }

        [Fact]
        public void Constructor_WhenSuccess_ShouldInitializeProperties()
        {
            this.subject.Collector.Should().NotBeNull();
            this.subject.Logger.Should().NotBeNull();
        }

        [Fact]
        public async Task RunAsync_WithValidArguments_ShouldInvokeLoggerAndCollector()
        {
            // Arrange.
            var service = typeof(string);
            var ex = new Exception();

            using (var cancellationToken = new CancellationTokenSource())
            {
                // Act.
                await ((IBackgroundServiceExceptionHandler)this.subject).RunAsync(service, ex, cancellationToken.Token);

                // Assert.
                this.logger.Verify(
                    l => l.Log(LogLevel.Critical, 0, It.Is<FormattedLogValues>(v => v != null), ex, It.Is<Func<object, Exception, string>>(v => v != null)),
                    Times.Once()
                );
                this.subject.Collector.Should().ContainSingle(e => e.Service == service && ReferenceEquals(e.Exception, ex));
            }
        }
    }
}
