using System;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Xunit;
using Ztm.ObjectModel;

namespace Ztm.ServiceModel.Tests
{
    public sealed class ServiceTests : IDisposable
    {
        readonly TestService subject;

        public ServiceTests()
        {
            this.subject = new TestService();
        }

        public void Dispose()
        {
            this.subject.Dispose();
        }

        [Fact]
        public async Task OnStoppedAsync_WhenInvoke_ShouldChangeIsRunningToFalseAndInvokeStoppedEvent()
        {
            using (var cancellationSource = new CancellationTokenSource())
            {
                // Arrange.
                var eventHandler = Substitute.For<EventHandler<AsyncEventArgs>>();

                this.subject.Stopped += eventHandler;

                // Act.
                await this.subject.OnStoppedAsync(cancellationSource.Token);

                // Assert.
                Assert.False(this.subject.IsRunning);

                eventHandler.Received(1).Invoke(
                    this.subject,
                    Arg.Is<AsyncEventArgs>(e => e != null && e.CancellationToken == cancellationSource.Token)
                );
            }
        }

        [Fact]
        public async Task OnStartedAsync_WhenInvoke_ShouldChangeIsRunningToTrueAndInvokeStartedEvent()
        {
            using (var cancellationSource = new CancellationTokenSource())
            {
                // Arrange.
                var eventHandler = Substitute.For<EventHandler<AsyncEventArgs>>();

                this.subject.Started += eventHandler;

                // Act.
                await this.subject.OnStartedAsync(cancellationSource.Token);

                // Assert.
                Assert.True(this.subject.IsRunning);

                eventHandler.Received(1).Invoke(
                    this.subject,
                    Arg.Is<AsyncEventArgs>(e => e != null && e.CancellationToken == cancellationSource.Token)
                );
            }
        }
    }
}
