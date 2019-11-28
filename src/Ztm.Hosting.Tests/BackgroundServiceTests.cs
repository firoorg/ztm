using System;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Xunit;
using Ztm.Testing;

namespace Ztm.Hosting.Tests
{
    public sealed class BackgroundServiceTests : IDisposable
    {
        readonly IBackgroundServiceExceptionHandler exceptionHandler;
        readonly FakeBackgroundService subject;

        public BackgroundServiceTests()
        {
            this.exceptionHandler = Substitute.For<IBackgroundServiceExceptionHandler>();
            this.exceptionHandler.RunAsync(Arg.Any<Type>(), Arg.Any<Exception>(), Arg.Any<CancellationToken>())
                                 .Returns(Task.CompletedTask);

            this.subject = new FakeBackgroundService(this.exceptionHandler);
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
            this.subject.StubbedDispose.Received(2)(true);
        }

        [Fact]
        public async Task Dispose_AlreadyStarted_ShouldInvokeStopAsync()
        {
            // Arrange.
            var cancel = Substitute.For<Action>();

            this.subject.StubbedExecuteAsync
                .When(f => f(Arg.Any<CancellationToken>()))
                .Do(call => call.ArgAt<CancellationToken>(0).Register(cancel));

            await this.subject.StartAsync(CancellationToken.None);

            // Act.
            this.subject.Dispose();
            this.subject.Dispose();

            // Assert.
            cancel.Received(1)();
            this.subject.StubbedDispose.Received(2)(true);
        }

        [Fact]
        public async Task StartAsync_NotStarted_ShouldStart()
        {
            await AsynchronousTesting.WithCancellationTokenAsync(async cancellationToken =>
            {
                // Act.
                await this.subject.StartAsync(cancellationToken);

                // Assert.
                _ = this.subject.StubbedExecuteAsync.Received(1)(
                    Arg.Is<CancellationToken>(t => t != cancellationToken)
                );

                _ = this.exceptionHandler.Received(0).RunAsync(
                    Arg.Any<Type>(),
                    Arg.Any<Exception>(),
                    Arg.Any<CancellationToken>()
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
                _ = this.subject.StubbedExecuteAsync.Received(1)(
                    Arg.Is<CancellationToken>(t => t != cancellationToken)
                );

                await Assert.ThrowsAsync<InvalidOperationException>(
                    () => this.subject.StartAsync(CancellationToken.None)
                );
            });
        }

        [Fact]
        public async Task StartAsync_WhenBackgroundTaskThrow_ShouldInvokeExceptionHandler()
        {
            await AsynchronousTesting.WithCancellationTokenAsync(async cancellationToken =>
            {
                // Arrange.
                this.subject.StubbedExecuteAsync(Arg.Any<CancellationToken>())
                            .Returns(Task.FromException(new Exception()));

                // Act.
                await this.subject.StartAsync(cancellationToken);

                // Assert.
                _ = this.subject.StubbedExecuteAsync.Received(1)(
                    Arg.Is<CancellationToken>(t => t != cancellationToken)
                );

                _ = this.exceptionHandler.Received(1).RunAsync(
                    this.subject.GetType(),
                    Arg.Is<Exception>(ex => ex != null),
                    Arg.Any<CancellationToken>()
                );
            });
        }

        [Fact]
        public async Task StopAsync_NotStarted_ShouldThrow()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => this.subject.StopAsync(CancellationToken.None));
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
                _ = this.subject.StubbedExecuteAsync.Received(1)(
                    Arg.Is<CancellationToken>(t => t != cancellationToken)
                );

                _ = this.exceptionHandler.Received(0).RunAsync(
                    Arg.Any<Type>(),
                    Arg.Any<Exception>(),
                    Arg.Any<CancellationToken>()
                );
            });
        }

        [Fact]
        public async Task StopAsync_BackgroundTaskThrowException_ShouldSuccess()
        {
            await AsynchronousTesting.WithCancellationTokenAsync(async cancellationToken =>
            {
                // Arrange.
                this.subject.StubbedExecuteAsync(Arg.Any<CancellationToken>())
                            .Returns(Task.FromException(new Exception()));

                await this.subject.StartAsync(cancellationToken);

                // Act.
                await this.subject.StopAsync(CancellationToken.None);

                // Assert.
                _ = this.subject.StubbedExecuteAsync.Received(1)(
                    Arg.Is<CancellationToken>(t => t != cancellationToken)
                );

                _ = this.exceptionHandler.Received(1).RunAsync(
                    this.subject.GetType(),
                    Arg.Is<Exception>(ex => ex != null),
                    Arg.Any<CancellationToken>()
                );
            });
        }

        [Fact]
        public async Task StopAsync_BackgroundTaskCanceled_ShouldSuccess()
        {
            await AsynchronousTesting.WithCancellationTokenAsync(async cancellationToken =>
            {
                // Arrange.
                this.subject.StubbedExecuteAsync(Arg.Any<CancellationToken>()).Returns(async call =>
                {
                    var token = call.ArgAt<CancellationToken>(0);
                    await Task.Delay(Timeout.InfiniteTimeSpan, token);
                    token.ThrowIfCancellationRequested();
                });

                await this.subject.StartAsync(cancellationToken);

                // Act.
                await this.subject.StopAsync(CancellationToken.None);

                // Assert.
                _ = this.subject.StubbedExecuteAsync.Received(1)(
                    Arg.Is<CancellationToken>(t => t != cancellationToken)
                );

                _ = this.exceptionHandler.Received(0).RunAsync(
                    Arg.Any<Type>(),
                    Arg.Any<Exception>(),
                    Arg.Any<CancellationToken>()
                );
            });
        }

        [Fact]
        public async Task StopAsync_WhenCanceled_ShouldThrow()
        {
            await AsynchronousTesting.WithCancellationTokenAsync(async (cancellationToken, cancel) =>
            {
                // Arrange.
                this.subject.StubbedExecuteAsync(Arg.Any<CancellationToken>()).Returns(async call =>
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan);
                });

                await this.subject.StartAsync(CancellationToken.None);

                // Act.
                var stop = this.subject.StopAsync(cancellationToken);

                cancel();

                // Assert.
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => stop);

                _ = this.subject.StubbedExecuteAsync.Received(1)(
                    Arg.Any<CancellationToken>()
                );

                _ = this.exceptionHandler.Received(0).RunAsync(
                    Arg.Any<Type>(),
                    Arg.Any<Exception>(),
                    Arg.Any<CancellationToken>()
                );
            }, cancellationSource => cancellationSource.Cancel());
        }
    }
}
