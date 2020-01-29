using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Ztm.Testing;

namespace Ztm.Threading.Tests
{
    public sealed class CancellationTokenExtenionsTests : IDisposable
    {
        readonly CancellationTokenSource source;

        public CancellationTokenExtenionsTests()
        {
            this.source = new CancellationTokenSource();
        }

        public void Dispose()
        {
            this.source.Dispose();
        }

        [Fact]
        public Task WaitAsync_OnNonCancelable_ShouldThrow()
        {
            var subject = CancellationToken.None;

            return Assert.ThrowsAsync<InvalidOperationException>(() => subject.WaitAsync(CancellationToken.None));
        }

        [Fact]
        public async Task WaitAsync_WithUncancelableCancellationToken_ShouldCompletedWhenCanceled()
        {
            // Act.
            var task = this.source.Token.WaitAsync(CancellationToken.None);

            this.source.Cancel();

            // Assert.
            await task;
        }

        [Fact]
        public Task WaitAsync_WithCancellationToken_ShouldCompletedWhenCanceled()
        {
            return AsynchronousTesting.WithCancellationTokenAsync(async cancellationToken =>
            {
                // Act.
                var task = this.source.Token.WaitAsync(cancellationToken);
                this.source.Cancel();

                // Assert.
                await task;
            });
        }

        [Fact]
        public Task WaitAsync_WithCancellationToken_ShouldCancelWhenCancellationTokenCanceled()
        {
            return AsynchronousTesting.WithCancellationTokenAsync(async (cancellationToken, cancel) =>
            {
                // Act.
                var task = this.source.Token.WaitAsync(cancellationToken);
                cancel();

                // Assert.
                await Assert.ThrowsAsync<TaskCanceledException>(() => task);
            }, source => source.Cancel());
        }
    }
}
