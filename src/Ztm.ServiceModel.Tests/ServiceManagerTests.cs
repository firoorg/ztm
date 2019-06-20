using System;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Xunit;
using Ztm.ObjectModel;

namespace Ztm.ServiceModel.Tests
{
    public sealed class ServiceManagerTests : IDisposable
    {
        readonly ServiceManager subject;

        public ServiceManagerTests()
        {
            this.subject = new ServiceManager();
        }

        public void Dispose()
        {
            this.subject.Dispose();
        }

        [Fact]
        public void Constructor_WithNullServices_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>("services", () => new ServiceManager(null));
        }

        [Fact]
        public void Constructor_WithServices_ShouldAddedThoseServices()
        {
            // Arrange.
            var service1 = Substitute.For<IBackgroundService>();
            var service2 = Substitute.For<IBackgroundService>();

            // Act.
            using (var subject = new ServiceManager(new[] { service1, service2 }))
            {
                // Assert.
                Assert.Equal(new[] { service1, service2 }, subject.Services);
            }
        }

        [Fact]
        public void Add_PassNullForService_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "service",
                () => this.subject.Add(null)
            );
        }

        [Fact]
        public void Add_WithAlreadyRunningService_ShouldThrow()
        {
            // Arrange.
            var service = Substitute.For<IService>();
            service.IsRunning.Returns(true);

            // Act.
            Assert.Throws<ArgumentException>(
                "service",
                () => this.subject.Add(service)
            );
        }

        [Fact]
        public void Add_WhenAlreadyAdded_ShouldThrow()
        {
            // Arrange.
            var service = Substitute.For<IService>();

            this.subject.Add(service);

            // Act.
            Assert.Throws<ArgumentException>(
                "service",
                () => this.subject.Add(service)
            );
        }

        [Fact]
        public async Task Add_AlreadyRunning_ShouldThrow()
        {
            // Arrange.
            var service = Substitute.For<IService>();

            await this.subject.StartAsync(CancellationToken.None);

            // Act.
            Assert.Throws<InvalidOperationException>(
                () => this.subject.Add(service)
            );
        }

        [Fact]
        public void Add_WithSameServiceButDifferentInstance_ShouldSuccess()
        {
            // Arrange.
            using (var service1 = new TestService())
            using (var service2 = new TestService())
            {
                // Act.
                this.subject.Add(service1);
                this.subject.Add(service2);

                // Assert.
                Assert.Equal(new[] { service1, service2 }, this.subject.Services);
            }
        }

        [Fact]
        public void Dispose_WhenInvoke_ShouldNotDisposeAnyServices()
        {
            // Arrange.
            var service1 = Substitute.For<IService>();
            var service2 = Substitute.For<IService>();

            this.subject.Add(service1);
            this.subject.Add(service2);

            // Act.
            this.subject.Dispose();

            // Assert.
            service1.Received(0).Dispose();
            service2.Received(0).Dispose();
        }

        [Fact]
        public void Remove_PassNullForService_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "service",
                () => this.subject.Remove(null)
            );
        }

        [Fact]
        public void Remove_NotAdded_ShouldThrow()
        {
            // Arrange.
            var service = Substitute.For<IService>();

            // Act.
            Assert.Throws<ArgumentException>(
                "service",
                () => this.subject.Remove(service)
            );
        }

        [Fact]
        public async Task Remove_AlreadyRunning_ShouldThrow()
        {
            // Arrange.
            var service = Substitute.For<IBackgroundService>();

            service.StartAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask).AndDoes(_ =>
            {
                service.IsRunning.Returns(true);
            });

            service.StopAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask).AndDoes(_ =>
            {
                service.IsRunning.Returns(false);
            });

            this.subject.Add(service);

            await this.subject.StartAsync(CancellationToken.None);

            // Act.
            Assert.Throws<InvalidOperationException>(
                () => this.subject.Remove(service)
            );
        }

        [Fact]
        public void Remove_AlreadyAdded_ShouldRemove()
        {
            // Arrange.
            var service1 = Substitute.For<IService>();
            var service2 = Substitute.For<IService>();

            this.subject.Add(service1);
            this.subject.Add(service2);

            // Act.
            this.subject.Remove(service1);

            // Assert.
            Assert.Equal(new[] { service2 }, this.subject.Services);
        }

        [Fact]
        public async Task StartAsync_SomeServicesFail_ShouldStopPreviouslyStarted()
        {
            // Arrange.
            var service1 = Substitute.For<IBackgroundService>();
            var service2 = Substitute.For<IBackgroundService>();
            var error = new Exception();

            service1.StartAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask).AndDoes(_ =>
            {
                service1.IsRunning.Returns(true);
            });

            service1.StopAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask).AndDoes(_ =>
            {
                service1.IsRunning.Returns(false);
            });

            service2.StartAsync(Arg.Any<CancellationToken>()).Returns(Task.FromException(error));

            this.subject.Add(service1);
            this.subject.Add(service2);

            // Act.
            var ex = await Assert.ThrowsAsync<Exception>(
                () => this.subject.StartAsync(CancellationToken.None)
            );

            // Assert.
            Assert.Same(error, ex);

            _ = service1.Received(1).StopAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task StartAsync_WhenSuccess_AllServicesShouldStarted()
        {
            // Arrange.
            var service1 = Substitute.For<IBackgroundService>();
            var service2 = Substitute.For<IBackgroundService>();

            service1.StartAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask).AndDoes(_ =>
            {
                service1.IsRunning.Returns(true);
            });

            service1.StopAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask).AndDoes(_ =>
            {
                service1.IsRunning.Returns(false);
            });

            service2.StartAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask).AndDoes(_ =>
            {
                service2.IsRunning.Returns(true);
            });

            service2.StopAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask).AndDoes(_ =>
            {
                service2.IsRunning.Returns(false);
            });

            this.subject.Add(service1);
            this.subject.Add(service2);

            // Act.
            await this.subject.StartAsync(CancellationToken.None);

            // Assert.
            Assert.True(service1.IsRunning);
            Assert.True(service2.IsRunning);
        }

        [Fact]
        public async Task StartAsync_SomeServicesStoppedFaulted_ShouldStopNowWithFaulted()
        {
            // Arrange.
            var service1 = Substitute.For<IBackgroundService>();
            var service2 = Substitute.For<IBackgroundService>();
            var error = new Exception();

            service1.StartAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask).AndDoes(_ =>
            {
                service1.IsRunning.Returns(true);
            });

            service2.StartAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask).AndDoes(_ =>
            {
                service2.IsRunning.Returns(true);
            });

            service2.StopAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask).AndDoes(_ =>
            {
                service2.IsRunning.Returns(false);
            });

            this.subject.Add(service1);
            this.subject.Add(service2);

            // Act.
            await this.subject.StartAsync(CancellationToken.None);

            service1.Exception.Returns(error);
            service1.IsRunning.Returns(false);
            service1.StopAsync(Arg.Any<CancellationToken>()).Returns(Task.FromException(new InvalidOperationException()));
            service1.Stopped += Raise.EventWith(new AsyncEventArgs(CancellationToken.None));

            await Task.Delay(1000);

            // Assert.
            Assert.False(this.subject.IsRunning);
            Assert.Same(error, this.subject.Exception);
        }

        [Fact]
        public async Task StartAsync_SomeServicesStoppedNonFaulted_ShouldStillRunning()
        {
            // Arrange.
            var service1 = Substitute.For<IBackgroundService>();
            var service2 = Substitute.For<IBackgroundService>();

            service1.StartAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask).AndDoes(_ =>
            {
                service1.IsRunning.Returns(true);
            });

            service2.StartAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask).AndDoes(_ =>
            {
                service2.IsRunning.Returns(true);
            });

            service2.StopAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask).AndDoes(_ =>
            {
                service2.IsRunning.Returns(false);
            });

            this.subject.Add(service1);
            this.subject.Add(service2);

            // Act.
            await this.subject.StartAsync(CancellationToken.None);

            service1.IsRunning.Returns(false);
            service1.StopAsync(Arg.Any<CancellationToken>()).Returns(Task.FromException(new InvalidOperationException()));
            service1.Stopped += Raise.EventWith(new AsyncEventArgs(CancellationToken.None));

            await Task.Delay(1000);

            // Assert.
            Assert.True(this.subject.IsRunning);
            Assert.Null(this.subject.Exception);
        }

        [Fact]
        public async Task StartAsync_AllServicesStoppedNonFaulted_ShouldStopWithNonFaulted()
        {
            // Arrange.
            var service1 = Substitute.For<IBackgroundService>();
            var service2 = Substitute.For<IBackgroundService>();

            service1.StartAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask).AndDoes(_ =>
            {
                service1.IsRunning.Returns(true);
            });

            service2.StartAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask).AndDoes(_ =>
            {
                service2.IsRunning.Returns(true);
            });

            this.subject.Add(service1);
            this.subject.Add(service2);

            // Act.
            await this.subject.StartAsync(CancellationToken.None);

            service1.IsRunning.Returns(false);
            service1.StopAsync(Arg.Any<CancellationToken>()).Returns(Task.FromException(new InvalidOperationException()));
            service1.Stopped += Raise.EventWith(new AsyncEventArgs(CancellationToken.None));

            service2.IsRunning.Returns(false);
            service2.StopAsync(Arg.Any<CancellationToken>()).Returns(Task.FromException(new InvalidOperationException()));
            service2.Stopped += Raise.EventWith(new AsyncEventArgs(CancellationToken.None));

            await Task.Delay(1000);

            // Assert.
            Assert.False(this.subject.IsRunning);
            Assert.Null(this.subject.Exception);
        }

        [Fact]
        public async Task StopAsync_SomeServicesAlreadyStopped_ShouldIgnoreIt()
        {
            // Arrange.
            var service1 = Substitute.For<IBackgroundService>();
            var service2 = Substitute.For<IBackgroundService>();

            service1.StartAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask).AndDoes(_ =>
            {
                service1.IsRunning.Returns(true);
            });

            service2.StartAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask).AndDoes(_ =>
            {
                service2.IsRunning.Returns(true);
            });

            service2.StopAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask).AndDoes(_ =>
            {
                service2.IsRunning.Returns(false);
            });

            this.subject.Add(service1);
            this.subject.Add(service2);

            await this.subject.StartAsync(CancellationToken.None);

            service1.IsRunning.Returns(false);
            service1.StopAsync(Arg.Any<CancellationToken>()).Returns(Task.FromException(new InvalidOperationException()));

            // Act.
            await this.subject.StopAsync(CancellationToken.None);

            // Assert.
            Assert.False(service2.IsRunning);
        }

        [Fact]
        public async Task StopAsync_WhenCancel_ShouldNotStopNextServices()
        {
            // Arrange.
            var service1 = Substitute.For<IBackgroundService>();
            var service2 = Substitute.For<IBackgroundService>();

            service1.StartAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask).AndDoes(_ =>
            {
                service1.IsRunning.Returns(true);
            });

            service1.StopAsync(Arg.Any<CancellationToken>()).Returns(call =>
            {
                call.ArgAt<CancellationToken>(0).ThrowIfCancellationRequested();
                service1.IsRunning.Returns(false);
                return Task.CompletedTask;
            });

            service2.StartAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask).AndDoes(_ =>
            {
                service2.IsRunning.Returns(true);
            });

            service2.StopAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask).AndDoes(_ =>
            {
                service2.IsRunning.Returns(false);
            });

            this.subject.Add(service1);
            this.subject.Add(service2);

            await this.subject.StartAsync(CancellationToken.None);

            using (var cancellationSource = new CancellationTokenSource())
            {
                cancellationSource.Cancel();

                // Act.
                await Assert.ThrowsAnyAsync<OperationCanceledException>(
                    () => this.subject.StopAsync(cancellationSource.Token)
                );
            }

            // Assert.
            Assert.True(service2.IsRunning);
        }

        [Fact]
        public async Task StopAsync_AllServicesStillRunning_AllServicesShouldStopped()
        {
            // Arrange.
            var service1 = Substitute.For<IBackgroundService>();
            var service2 = Substitute.For<IBackgroundService>();

            service1.StartAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask).AndDoes(_ =>
            {
                service1.IsRunning.Returns(true);
            });

            service1.StopAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask).AndDoes(_ =>
            {
                service1.IsRunning.Returns(false);
            });

            service2.StartAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask).AndDoes(_ =>
            {
                service2.IsRunning.Returns(true);
            });

            service2.StopAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask).AndDoes(_ =>
            {
                service2.IsRunning.Returns(false);
            });

            this.subject.Add(service1);
            this.subject.Add(service2);

            await this.subject.StartAsync(CancellationToken.None);

            // Act.
            await this.subject.StopAsync(CancellationToken.None);

            // Assert.
            Assert.False(service1.IsRunning);
            Assert.False(service2.IsRunning);
        }
    }
}
