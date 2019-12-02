using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Internal;
using Moq;
using Xunit;

namespace Ztm.Hosting.Tests
{
    public sealed class BackgroundServiceErrorLoggerTests
    {
        readonly Mock<ILogger<BackgroundServiceErrorLogger>> logger;
        readonly BackgroundServiceErrorLogger subject;

        public BackgroundServiceErrorLoggerTests()
        {
            this.logger = new Mock<ILogger<BackgroundServiceErrorLogger>>();
            this.subject = new BackgroundServiceErrorLogger(this.logger.Object);
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>("logger", () => new BackgroundServiceErrorLogger(null));
        }

        [Fact]
        public async Task RunAsync_WithValidArguments_ShouldInvokeLogger()
        {
            var ex = new Exception();

            await ((IBackgroundServiceExceptionHandler)this.subject).RunAsync(
                typeof(FakeBackgroundService),
                ex,
            CancellationToken.None);

            this.logger.Verify(
                l => l.Log(
                    LogLevel.Critical,
                    0,
                    It.IsNotNull<FormattedLogValues>(),
                    ex,
                    It.IsNotNull<Func<object, Exception, string>>()
                ),
                Times.Once()
            );
        }
    }
}
