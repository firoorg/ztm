using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ztm.ObjectModel.Tests
{
    public class EventHandlerExtensionsTests
    {
        event EventHandler<AsyncEventArgs> BaseEvent;
        event EventHandler<DerivedEventArgs> DerivedEvent;

        [Fact]
        public void InvokeAsync_BaseEventArgsWithNoSubscribers_ShouldReturnCompletedTask()
        {
            var task = BaseEvent.InvokeAsync(this, new AsyncEventArgs(CancellationToken.None));

            Assert.Same(Task.CompletedTask, task);
        }

        [Fact]
        public async Task InvokeAsync_BaseEventArgsWithNonBackgroundTasks_ShouldCompleteSucceeded()
        {
            // Arrange.
            BaseEvent += (sender, e) =>
            {
            };

            // Act.
            var args = new AsyncEventArgs(CancellationToken.None);
            await BaseEvent.InvokeAsync(this, args);
        }

        [Fact]
        public async Task InvokeAsync_BaseEventArgsWithSucceededBackgroundTasks_ShouldCompleteSucceeded()
        {
            // Arrange.
            BaseEvent += (sender, e) => e.RegisterBackgroundTask(Task.CompletedTask);

            // Act.
            var args = new AsyncEventArgs(CancellationToken.None);
            await BaseEvent.InvokeAsync(this, args);
        }

        [Fact]
        public async Task InvokeAsync_BaseEventArgsWithCancelledBackgroundTasks_ShouldCompleteCancelled()
        {
            // Arrange.
            BaseEvent += (sender, e) => e.RegisterBackgroundTask(Task.CompletedTask);
            BaseEvent += (sender, e) => e.RegisterBackgroundTask(async cancellationToken =>
            {
                await Task.Yield();

                cancellationToken.ThrowIfCancellationRequested();
            });

            using (var cancellationSource = new CancellationTokenSource())
            {
                cancellationSource.Cancel();

                // Act.
                var args = new AsyncEventArgs(cancellationSource.Token);
                var error = await Assert.ThrowsAsync<OperationCanceledException>(() => BaseEvent.InvokeAsync(this, args));

                // Assert.
                Assert.Equal(cancellationSource.Token, error.CancellationToken);
            }
        }

        [Fact]
        public async Task InvokeAsync_BaseEventArgsWithFaultedBackgroundTasks_ShouldCompleteFaulted()
        {
            // Arrange.
            var error = new Exception();

            BaseEvent += (sender, e) => e.RegisterBackgroundTask(Task.CompletedTask);
            BaseEvent += (sender, e) => e.RegisterBackgroundTask(async cancellationToken =>
            {
                await Task.Yield();

                cancellationToken.ThrowIfCancellationRequested();
            });
            BaseEvent += (sender, e) => e.RegisterBackgroundTask(async cancellationToken =>
            {
                await Task.Yield();

                throw error;
            });

            using (var cancellationSource = new CancellationTokenSource())
            {
                cancellationSource.Cancel();

                // Act.
                var args = new AsyncEventArgs(cancellationSource.Token);
                var thrown = await Assert.ThrowsAsync<Exception>(() => BaseEvent.InvokeAsync(this, args));

                // Assert.
                Assert.Equal(error, thrown);
            }
        }

        [Fact]
        public void InvokeAsync_DerivedEventArgsWithNoSubscribers_ShouldReturnCompletedTask()
        {
            var task = DerivedEvent.InvokeAsync(this, new DerivedEventArgs(CancellationToken.None));

            Assert.Same(Task.CompletedTask, task);
        }

        [Fact]
        public async Task InvokeAsync_DerivedEventArgsWithNonBackgroundTasks_ShouldCompleteSucceeded()
        {
            // Arrange.
            DerivedEvent += (sender, e) =>
            {
            };

            // Act.
            var args = new DerivedEventArgs(CancellationToken.None);
            await DerivedEvent.InvokeAsync(this, args);
        }

        [Fact]
        public async Task InvokeAsync_DerivedEventArgsWithSucceededBackgroundTasks_ShouldCompleteSucceeded()
        {
            // Arrange.
            DerivedEvent += (sender, e) => e.RegisterBackgroundTask(Task.CompletedTask);

            // Act.
            var args = new DerivedEventArgs(CancellationToken.None);
            await DerivedEvent.InvokeAsync(this, args);
        }

        [Fact]
        public async Task InvokeAsync_DerivedEventArgsWithCancelledBackgroundTasks_ShouldCompleteCancelled()
        {
            // Arrange.
            DerivedEvent += (sender, e) => e.RegisterBackgroundTask(Task.CompletedTask);
            DerivedEvent += (sender, e) => e.RegisterBackgroundTask(async cancellationToken =>
            {
                await Task.Yield();

                cancellationToken.ThrowIfCancellationRequested();
            });

            using (var cancellationSource = new CancellationTokenSource())
            {
                cancellationSource.Cancel();

                // Act.
                var args = new DerivedEventArgs(cancellationSource.Token);
                var error = await Assert.ThrowsAsync<OperationCanceledException>(() => DerivedEvent.InvokeAsync(this, args));

                // Assert.
                Assert.Equal(cancellationSource.Token, error.CancellationToken);
            }
        }

        [Fact]
        public async Task InvokeAsync_DerivedEventArgsWithFaultedBackgroundTasks_ShouldCompleteFaulted()
        {
            // Arrange.
            var error = new Exception();

            DerivedEvent += (sender, e) => e.RegisterBackgroundTask(Task.CompletedTask);
            DerivedEvent += (sender, e) => e.RegisterBackgroundTask(async cancellationToken =>
            {
                await Task.Yield();

                cancellationToken.ThrowIfCancellationRequested();
            });
            DerivedEvent += (sender, e) => e.RegisterBackgroundTask(async cancellationToken =>
            {
                await Task.Yield();

                throw error;
            });

            using (var cancellationSource = new CancellationTokenSource())
            {
                cancellationSource.Cancel();

                // Act.
                var args = new DerivedEventArgs(cancellationSource.Token);
                var thrown = await Assert.ThrowsAsync<Exception>(() => DerivedEvent.InvokeAsync(this, args));

                // Assert.
                Assert.Equal(error, thrown);
            }
        }

        class DerivedEventArgs : AsyncEventArgs
        {
            public DerivedEventArgs(CancellationToken cancellationToken) : base(cancellationToken)
            {
            }
        }
    }
}
