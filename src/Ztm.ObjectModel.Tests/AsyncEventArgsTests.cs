using System;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Xunit;

namespace Ztm.ObjectModel.Tests
{
    public class AsyncEventArgsTests
    {
        readonly AsyncEventArgs subject;

        public AsyncEventArgsTests()
        {
            this.subject = new AsyncEventArgs(CancellationToken.None);
        }

        [Fact]
        public void Constructor_PassSpecificCancellationToken_ShouldAssignToCancellationToken()
        {
            using (var cancellationSource = new CancellationTokenSource())
            {
                var subject = new AsyncEventArgs(cancellationSource.Token);

                Assert.Equal(cancellationSource.Token, subject.CancellationToken);
            }
        }

        [Fact]
        public void RegisterBackgroundTask_PassNullForTask_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>("task", () => this.subject.RegisterBackgroundTask(task: null));
        }

        [Fact]
        public void RegisterBackgroundTask_PassNonNullForTask_ShouldAddedToBackgroundTasks()
        {
            this.subject.RegisterBackgroundTask(Task.CompletedTask);

            Assert.Single(this.subject.BackgroundTasks, Task.CompletedTask);
        }

        [Fact]
        public void RegisterBackgroundTask_PassNullForFunc_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>("func", () => this.subject.RegisterBackgroundTask(func: null));
        }

        [Fact]
        public void RegisterBackgroundTask_PassNonNullForFunc_ShouldCallAddedResultedTaskToBackgroundTasks()
        {
            // Arrange.
            var func = Substitute.For<Func<CancellationToken, Task>>();
            func(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

            // Act.
            this.subject.RegisterBackgroundTask(func);

            // Assert.
            Assert.Single(this.subject.BackgroundTasks, Task.CompletedTask);

            func.Received(1)(Arg.Any<CancellationToken>());
        }

        [Fact]
        public void RegisterBackgroundTask_PassNonNullForFunc_ShouldPassCancellationTokenToFunc()
        {
            // Arrange.
            var func = Substitute.For<Func<CancellationToken, Task>>();
            func(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

            using (var cancellationSource = new CancellationTokenSource())
            {
                // Act.
                var subject = new AsyncEventArgs(cancellationSource.Token);
                subject.RegisterBackgroundTask(func);

                // Assert.
                func.Received(1)(cancellationSource.Token);
            }
        }
    }
}
