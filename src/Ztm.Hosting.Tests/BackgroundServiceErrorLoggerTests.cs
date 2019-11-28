using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
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
        public void RunAsync_WithNullService_ShouldThrow()
        {
            var ex = new Exception();

            this.subject.Invoking(s => s.RunAsync(null, ex, CancellationToken.None))
                        .Should().ThrowExactly<ArgumentNullException>()
                        .And.ParamName.Should().Be("service");
        }

        [Fact]
        public void RunAsync_WithNullException_ShouldThrow()
        {
            this.subject.Invoking(s => s.RunAsync(typeof(FakeBackgroundService), null, CancellationToken.None))
                        .Should().ThrowExactly<ArgumentNullException>()
                        .And.ParamName.Should().Be("exception");
        }

        [Fact]
        public async Task RunAsync_WithValidArguments_ShouldInvokeLogger()
        {
            var ex = new Exception();

            await this.subject.RunAsync(typeof(FakeBackgroundService), ex, CancellationToken.None);

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
