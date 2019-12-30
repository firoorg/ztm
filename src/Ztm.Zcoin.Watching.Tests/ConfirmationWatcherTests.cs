using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NBitcoin;
using Xunit;
using Ztm.Testing;
using Ztm.Zcoin.NBitcoin;
using Ztm.Zcoin.Synchronization;

namespace Ztm.Zcoin.Watching.Tests
{
    public sealed class ConfirmationWatcherTests
    {
        readonly Mock<IConfirmationWatcherHandler<object, Watch<object>, object>> handler;
        readonly Mock<IBlocksStorage> blocks;
        readonly FakeConfirmationWatcher subject;

        public ConfirmationWatcherTests()
        {
            this.handler = new Mock<IConfirmationWatcherHandler<object, Watch<object>, object>>();
            this.blocks = new Mock<IBlocksStorage>();
            this.subject = new FakeConfirmationWatcher(this.handler.Object, this.blocks.Object);
        }

        [Fact]
        public void Constructor_WithNullHandler_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "handler",
                () => new FakeConfirmationWatcher(null, this.blocks.Object)
            );
        }

        [Fact]
        public void Constructor_WithNullBlocks_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "blocks",
                () => new FakeConfirmationWatcher(this.handler.Object, null)
            );
        }

        [Fact]
        public Task ExecuteAsync_WhenGetWatchesAsyncInvoked_ShouldInvokeGetCurrentWatchesAsync()
        {
            return AsynchronousTesting.WithCancellationTokenAsync(async cancellationToken =>
            {
                // Arrange.
                var block = ZcoinNetworks.Instance.Regtest.GetGenesis();

                // Act.
                await this.subject.ExecuteAsync(block, 0, BlockEventType.Added, cancellationToken);

                // Assert.
                this.handler.Verify(
                    h => h.GetCurrentWatchesAsync(cancellationToken),
                    Times.Once()
                );
            });
        }

        [Theory]
        [InlineData(BlockEventType.Added, ConfirmationType.Confirmed)]
        [InlineData(BlockEventType.Removing, ConfirmationType.Unconfirming)]
        public void GetConfirmationType_WithValidBlockEventType_ShouldSuccess(BlockEventType eventType, ConfirmationType expected)
        {
            var result = FakeConfirmationWatcher.GetConfirmationType(eventType);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetConfirmationType_WithInvalidBlockEventType_ShouldThrow()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                "eventType",
                () => FakeConfirmationWatcher.GetConfirmationType((BlockEventType)100)
            );
        }

        [Fact]
        public Task GetConfirmationAsync_WithNullWatch_ShouldThrow()
        {
            return Assert.ThrowsAsync<ArgumentNullException>(
                "watch",
                () => this.subject.GetConfirmationAsync(null, 0, CancellationToken.None)
            );
        }

        [Theory]
        [InlineData(-1, 0)]
        [InlineData(0, 1)]
        [InlineData(1, 2)]
        public Task GetConfirmationAsync_WithInvalidCurrentHeight_ShouldThrow(int currentHeight, int watchHeight)
        {
            // Arrange.
            var watch = new Watch<object>(null, uint256.One);

            this.blocks.Setup(b => b.GetAsync(watch.StartBlock, It.IsAny<CancellationToken>()))
                       .Returns(Task.FromResult(((Block)null, watchHeight)));

            // Act.
            return Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                "currentHeight",
                () => this.subject.GetConfirmationAsync(watch, currentHeight, CancellationToken.None)
            );
        }

        [Theory]
        [InlineData(0, 0, 1)]
        [InlineData(1, 0, 2)]
        [InlineData(2, 0, 3)]
        [InlineData(2, 1, 2)]
        public Task GetConfirmationAsync_WithValidArgs_ShouldReturnCorrectConfirmation(int currentHeight, int watchHeight, int expectedHeight)
        {
            return AsynchronousTesting.WithCancellationTokenAsync(async cancellationToken =>
            {
                // Arrange.
                var watch = new Watch<object>(null, uint256.One);

                this.blocks.Setup(b => b.GetAsync(watch.StartBlock, It.IsAny<CancellationToken>()))
                           .Returns(Task.FromResult(((Block)null, watchHeight)));

                // Act.
                var result = await this.subject.GetConfirmationAsync(watch, currentHeight, cancellationToken);

                // Assert.
                this.blocks.Verify(
                    b => b.GetAsync(watch.StartBlock, cancellationToken),
                    Times.Once()
                );

                Assert.Equal(expectedHeight, result);
            });
        }
    }
}
