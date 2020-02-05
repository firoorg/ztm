using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;
using Subject= Ztm.Testing.AsynchronousTesting;

namespace Ztm.Testing.Tests
{
    public sealed class AsynchronousTestingTests
    {
        [Fact]
        public void WithCancellationToken_WithNullTest_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>("test", () => Subject.WithCancellationToken(null));
        }

        [Fact]
        public void WithCancellationToken_WithNonNullTest_ShouldInvokeIt()
        {
            var test = new Mock<Action<CancellationToken>>();

            Subject.WithCancellationToken(test.Object);

            test.Verify(f => f(It.IsNotIn(CancellationToken.None)), Times.Once());
        }

        [Fact]
        public async Task WithCancellationTokenAsync_WithNullTest_ShouldThrow()
        {
            await Assert.ThrowsAsync<ArgumentNullException>("test", () => Subject.WithCancellationTokenAsync(null));

            await Assert.ThrowsAsync<ArgumentNullException>(
                "test",
                () => Subject.WithCancellationTokenAsync(null, cancellationToken => {}));

            await Assert.ThrowsAsync<ArgumentNullException>(
                "test",
                () => Subject.WithCancellationTokenAsync(null, cancellationToken => Task.CompletedTask));
        }

        [Fact]
        public async Task WithCancellationTokenAsync_WithNonNullTest_ShouldInvokeIt()
        {
            var test1 = new Mock<Func<CancellationToken, Task>>();
            var test2 = new Mock<Func<CancellationToken, Action, Task>>();
            var test3 = new Mock<Func<CancellationToken, Func<Task>, Task>>();

            await Subject.WithCancellationTokenAsync(test1.Object);
            await Subject.WithCancellationTokenAsync(test2.Object, cancellationToken => {});
            await Subject.WithCancellationTokenAsync(test3.Object, cancellationToken => Task.CompletedTask);

            test1.Verify(f => f(It.IsNotIn(CancellationToken.None)), Times.Once());
            test2.Verify(f => f(It.IsNotIn(CancellationToken.None), It.IsAny<Action>()), Times.Once());
            test3.Verify(f => f(It.IsNotIn(CancellationToken.None), It.IsAny<Func<Task>>()), Times.Once());
        }

        [Fact]
        public async Task WithCancellationTokenAsync_WithNullCancel_ShouldThrow()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                "cancel",
                () => Subject.WithCancellationTokenAsync(
                    (CancellationToken cancellationToken, Action cancel) => Task.CompletedTask,
                    null));

            await Assert.ThrowsAsync<ArgumentNullException>(
                "cancel",
                () => Subject.WithCancellationTokenAsync(
                    (CancellationToken cancellationToken, Func<Task> cancel) => Task.CompletedTask,
                    null));
        }

        [Fact]
        public async Task WithCancellationTokenAsync_WithNonNullCancel_ShouldInvokeTestWithCancelFunction()
        {
            await Subject.WithCancellationTokenAsync(
                (cancellationToken, cancel) =>
                {
                    Assert.False(cancellationToken.IsCancellationRequested);
                    cancel();
                    Assert.True(cancellationToken.IsCancellationRequested);

                    return Task.CompletedTask;
                },
                cancellationToken => cancellationToken.Cancel());

            await Subject.WithCancellationTokenAsync(
                async (cancellationToken, cancel) =>
                {
                    Assert.False(cancellationToken.IsCancellationRequested);
                    await cancel();
                    Assert.True(cancellationToken.IsCancellationRequested);
                },
                cancellationToken =>
                {
                    cancellationToken.Cancel();
                    return Task.CompletedTask;
                });
        }
    }
}
