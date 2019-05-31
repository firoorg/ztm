using System;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Xunit;
using Ztm.ObjectModel;

namespace Ztm.ServiceModel.Tests
{
    public class BackgroundServiceTests : IDisposable
    {
        readonly TestBackgroundService subject;

        public BackgroundServiceTests()
        {
            this.subject = new TestBackgroundService();
        }

        public void Dispose()
        {
            this.subject.Dispose();
        }

        [Fact]
        public async Task BeginStop_WhenInvoke_ShouldCallStopAsync()
        {
            // Arrange.
            await this.subject.StartAsync(CancellationToken.None);

            // Act.
            this.subject.BeginStop();
            await Task.Delay(1000);

            // Assert.
            Assert.False(this.subject.IsRunning);
        }

        [Fact]
        public async Task Dispose_WhenRunning_ShouldStop()
        {
            // Arrange.
            await this.subject.StartAsync(CancellationToken.None);

            // Act.
            this.subject.Dispose();

            // Assert.
            Assert.False(this.subject.IsRunning);
        }

        [Fact]
        public async Task StartAsync_AlreadyDisposed_ShouldThrow()
        {
            // Arrange.
            this.subject.Dispose();

            // Act.
            var ex = await Assert.ThrowsAsync<ObjectDisposedException>(
                () => this.subject.StartAsync(CancellationToken.None)
            );

            // Assert.
            Assert.Equal(this.subject.GetType().FullName, ex.ObjectName);
        }

        [Fact]
        public async Task StartAsync_AlreadyRunning_ShouldThrow()
        {
            // Arrange.
            await this.subject.StartAsync(CancellationToken.None);

            // Act.
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => this.subject.StartAsync(CancellationToken.None)
            );
        }

        [Fact]
        public async Task StartAsync_WhenInvoke_ShouldCallOnStartAsyncAndRaiseStarted()
        {
            // Arrange.
            var startedHandler = Substitute.For<EventHandler<AsyncEventArgs>>();

            this.subject.Started += startedHandler;

            using (var cancellationSource = new CancellationTokenSource())
            {
                // Act.
                await this.subject.StartAsync(cancellationSource.Token);

                // Assert.
                Assert.Equal(1, this.subject.OnStartAsyncCount);
                Assert.True(this.subject.IsRunning);

                startedHandler.Received(1).Invoke(
                    this.subject,
                    Arg.Is<AsyncEventArgs>(e => e.CancellationToken == cancellationSource.Token)
                );
            }
        }

        [Fact]
        public async Task StartAsync_StartedSubscriberThrow_ShouldStillRunning()
        {
            // Arrange.
            var error = new Exception();

            this.subject.Started += (sender, e) => throw error;

            // Act.
            var ex = await Assert.ThrowsAsync<Exception>(
                () => this.subject.StartAsync(CancellationToken.None)
            );

            // Assert.
            Assert.Same(error, ex);
            Assert.True(this.subject.IsRunning);
        }

        [Fact]
        public async Task StopAsync_AlreadyDisposed_ShouldThrow()
        {
            // Arrange.
            this.subject.Dispose();

            // Act.
            var ex = await Assert.ThrowsAsync<ObjectDisposedException>(
                () => this.subject.StopAsync(CancellationToken.None)
            );

            // Assert.
            Assert.Equal(this.subject.GetType().FullName, ex.ObjectName);
        }

        [Fact]
        public async Task StopAsync_IsNotRunning_ShouldThrow()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => this.subject.StopAsync(CancellationToken.None)
            );
        }

        [Fact]
        public async Task StopAsync_WhenInvoke_ShouldCallOnStopAsyncThenRaiseStopped()
        {
            // Arrange.
            var stoppedHandler = Substitute.For<EventHandler<AsyncEventArgs>>();

            this.subject.Stopped += stoppedHandler;

            await this.subject.StartAsync(CancellationToken.None);

            using (var cancellationSource = new CancellationTokenSource())
            {
                // Act.
                await this.subject.StopAsync(cancellationSource.Token);

                // Assert.
                Assert.Equal(1, this.subject.OnStopAsyncCount);
                Assert.False(this.subject.IsRunning);

                stoppedHandler.Received(1).Invoke(
                    this.subject,
                    Arg.Is<AsyncEventArgs>(e => e != null && e.CancellationToken == cancellationSource.Token)
                );
            }
        }

        [Fact]
        public async Task StopAsync_StoppedSubscriberThrow_ShouldStillStoppedSuccessfully()
        {
            // Arrange.
            var error = new Exception();

            this.subject.Stopped += (sender, e) => throw error;

            await this.subject.StartAsync(CancellationToken.None);

            // Act.
            var ex = await Assert.ThrowsAsync<Exception>(
                () => this.subject.StopAsync(CancellationToken.None)
            );

            // Assert.
            Assert.Same(error, ex);
            Assert.False(this.subject.IsRunning);
        }
    }
}
