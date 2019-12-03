using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;
using Ztm.Testing;

namespace Ztm.Hosting.Tests
{
    public sealed class BackgroundServiceExceptionHandlerTests
    {
        readonly FakeBackgroundServiceExceptionHandler subject;

        public BackgroundServiceExceptionHandlerTests()
        {
            this.subject = new FakeBackgroundServiceExceptionHandler();
        }

        [Fact]
        public async Task RunAsync_WithNullService_ShouldThrow()
        {
            var exception = new Exception();

            await Assert.ThrowsAsync<ArgumentNullException>(
                "service",
                () => this.subject.InvokeRunAsync(null, exception, CancellationToken.None)
            );
        }

        [Fact]
        public async Task RunAsync_WithNullException_ShouldThrow()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                "exception",
                () => this.subject.InvokeRunAsync(typeof(FakeBackgroundService), null, CancellationToken.None)
            );
        }

        [Fact]
        public Task RunAsync_WithValidArgs_ShouldInvokeProtectedRunAsync()
        {
            return AsynchronousTesting.WithCancellationTokenAsync(async cancellationToken =>
            {
                // Arrange.
                var exception = new Exception();

                // Act.
                await this.subject.InvokeRunAsync(typeof(FakeBackgroundService), exception, cancellationToken);

                // Assert.
                this.subject.StubbedRunAsync.Verify(
                    f => f(typeof(FakeBackgroundService), exception, cancellationToken),
                    Times.Once()
                );
            });
        }
    }
}
