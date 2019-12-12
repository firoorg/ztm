using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;
using Ztm.Testing;

namespace Ztm.Hosting.Tests
{
    public sealed class BackgroundServiceTests : IDisposable
    {
        readonly Mock<IBackgroundServiceExceptionHandler> exceptionHandler;
        readonly FakeBackgroundService subject;

        public BackgroundServiceTests()
        {
            this.exceptionHandler = new Mock<IBackgroundServiceExceptionHandler>();
            this.subject = new FakeBackgroundService(this.exceptionHandler.Object);
        }

        public void Dispose()
        {
            this.subject.Dispose();
        }

        [Fact]
        public void Constructor_WithNullExceptionHandler_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>("exceptionHandler", () => new FakeBackgroundService(null));
        }

        [Fact]
        public void Dispose_NotStarted_ShouldSuccess()
        {
            // Act.
            this.subject.Dispose();
            this.subject.Dispose();

            // Assert.
            this.subject.StubbedDispose.Verify(f => f(true), Times.Exactly(2));
        }

        [Fact]
        public async Task Dispose_AlreadyStarted_ShouldInvokeStopAsync()
        {
            // Arrange.
            var cancel = new Mock<Action>();

            this.subject.StubbedExecuteAsync.Setup(f => f(It.IsAny<CancellationToken>()))
                                            .Callback<CancellationToken>(cancellationToken =>
                                            {
                                                cancellationToken.Register(cancel.Object);
                                            });

            await this.subject.StartAsync(CancellationToken.None);

            // Act.
            this.subject.Dispose();
            this.subject.Dispose();

            // Assert.
            cancel.Verify(f => f(), Times.Once());
            this.subject.StubbedDispose.Verify(f => f(true), Times.Exactly(2));
        }

        [Fact]
        public async Task StartAsync_NotStarted_ShouldStart()
        {
            await AsynchronousTesting.WithCancellationTokenAsync(async cancellationToken =>
            {
                // Act.
                await this.subject.StartAsync(cancellationToken);
                await this.subject.StopAsync(CancellationToken.None);

                // Assert.
                this.subject.StubbedExecuteAsync.Verify(
                    f => f(It.Is<CancellationToken>(t => t != cancellationToken)),
                    Times.Once()
                );

                this.exceptionHandler.Verify(
                    h => h.RunAsync(It.IsAny<Type>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()),
                    Times.Never()
                );
            });
        }

        [Fact]
        public async Task StartAsync_AlreadyStarted_ShouldThrow()
        {
            await AsynchronousTesting.WithCancellationTokenAsync(async cancellationToken =>
            {
                // Act.
                await this.subject.StartAsync(cancellationToken);

                // Assert.
                this.subject.Invoking(s => s.StartAsync(CancellationToken.None))
                            .Should().ThrowExactly<InvalidOperationException>();
            });
        }

        [Fact]
        public async Task StartAsync_WhenBackgroundTaskThrow_ShouldInvokeExceptionHandler()
        {
            await AsynchronousTesting.WithCancellationTokenAsync(async cancellationToken =>
            {
                // Arrange.
                this.subject.StubbedExecuteAsync.Setup(f => f(It.IsAny<CancellationToken>()))
                                                .Returns(Task.FromException(new Exception()));

                // Act.
                await this.subject.StartAsync(cancellationToken);
                await this.subject.StopAsync(CancellationToken.None);

                // Assert.
                this.subject.StubbedExecuteAsync.Verify(
                    f => f(It.Is<CancellationToken>(t => t != cancellationToken)),
                    Times.Once()
                );

                this.exceptionHandler.Verify(
                    h => h.RunAsync(this.subject.GetType(), It.Is<Exception>(ex => ex != null), CancellationToken.None),
                    Times.Once()
                );
            });
        }

        [Fact]
        public void StopAsync_NotStarted_ShouldThrow()
        {
            this.subject.Invoking(s => s.StopAsync(CancellationToken.None))
                        .Should().ThrowExactly<InvalidOperationException>();
        }

        [Fact]
        public async Task StopAsync_BackgroundTaskSucceeded_ShouldSuccess()
        {
            await AsynchronousTesting.WithCancellationTokenAsync(async cancellationToken =>
            {
                // Arrange.
                await this.subject.StartAsync(cancellationToken);

                // Act.
                await this.subject.StopAsync(CancellationToken.None);

                // Assert.
                this.subject.StubbedExecuteAsync.Verify(
                    f => f(It.Is<CancellationToken>(t => t != cancellationToken)),
                    Times.Once()
                );

                this.exceptionHandler.Verify(
                    h => h.RunAsync(It.IsAny<Type>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()),
                    Times.Never()
                );
            });
        }

        [Fact]
        public async Task StopAsync_BackgroundTaskThrowException_ShouldSuccess()
        {
            await AsynchronousTesting.WithCancellationTokenAsync(async cancellationToken =>
            {
                // Arrange.
                this.subject.StubbedExecuteAsync.Setup(f => f(It.IsAny<CancellationToken>()))
                                                .Returns(Task.FromException(new Exception()));

                await this.subject.StartAsync(cancellationToken);

                // Act.
                await this.subject.StopAsync(CancellationToken.None);

                // Assert.
                this.subject.StubbedExecuteAsync.Verify(
                    f => f(It.Is<CancellationToken>(t => t != cancellationToken)),
                    Times.Once()
                );

                this.exceptionHandler.Verify(
                    h => h.RunAsync(this.subject.GetType(), It.Is<Exception>(ex => ex != null), CancellationToken.None),
                    Times.Once()
                );
            });
        }

        [Fact]
        public async Task StopAsync_BackgroundTaskCanceled_ShouldSuccess()
        {
            await AsynchronousTesting.WithCancellationTokenAsync(async cancellationToken =>
            {
                // Arrange.
                this.subject.StubbedExecuteAsync.Setup(f => f(It.IsAny<CancellationToken>()))
                                                .Returns<CancellationToken>(async t =>
                                                {
                                                    await Task.Delay(Timeout.InfiniteTimeSpan, t);
                                                    t.ThrowIfCancellationRequested();
                                                });

                await this.subject.StartAsync(cancellationToken);

                // Act.
                await this.subject.StopAsync(CancellationToken.None);

                // Assert.
                this.subject.StubbedExecuteAsync.Verify(
                    f => f(It.Is<CancellationToken>(t => t != cancellationToken)),
                    Times.Once()
                );

                this.exceptionHandler.Verify(
                    h => h.RunAsync(It.IsAny<Type>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()),
                    Times.Never()
                );
            });
        }

        [Fact]
        public async Task StopAsync_WhenCanceled_ShouldThrow()
        {
            await AsynchronousTesting.WithCancellationTokenAsync(async (cancellationToken, cancel) =>
            {
                // Arrange.
                this.subject.StubbedExecuteAsync.Setup(f => f(It.IsAny<CancellationToken>()))
                                                .Returns(Task.Delay(Timeout.InfiniteTimeSpan));

                await this.subject.StartAsync(CancellationToken.None);

                // Act.
                var stop = this.subject.StopAsync(cancellationToken);

                cancel();

                // Assert.
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => stop);
            }, cancellationSource => cancellationSource.Cancel());
        }
    }
}
