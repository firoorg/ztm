using System;
using System.Collections.Generic;
using System.Linq;
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
    public sealed class TransactionConfirmationWatcherTests
    {
        readonly Block block;
        readonly Mock<ITransactionConfirmationWatcherHandler<object>> handler;
        readonly Mock<IBlocksStorage> blocks;
        readonly TransactionConfirmationWatcher<object> subject;

        public TransactionConfirmationWatcherTests()
        {
            this.block = ZcoinNetworks.Instance.Regtest.GetGenesis();
            this.handler = new Mock<ITransactionConfirmationWatcherHandler<object>>();
            this.blocks = new Mock<IBlocksStorage>();
            this.subject = new TransactionConfirmationWatcher<object>(this.handler.Object, this.blocks.Object);
        }

        [Fact]
        public void Constructor_WithNullHandler_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "handler",
                () => new TransactionConfirmationWatcher<object>(null, this.blocks.Object)
            );
        }

        [Fact]
        public Task ExecuteAsync_CreateContextsAsyncReturnEmptyList_ShouldNotCreateAnyWatches()
        {
            return AsynchronousTesting.WithCancellationTokenAsync(async cancellationToken =>
            {
                // Arrange.
                this.handler.Setup(h => h.CreateContextsAsync(this.block.Transactions[0], It.IsAny<CancellationToken>()))
                            .Returns(Task.FromResult(Enumerable.Empty<object>()));

                // Act.
                await this.subject.ExecuteAsync(this.block, 0, BlockEventType.Added, cancellationToken);

                // Assert.
                this.handler.Verify(
                    h => h.CreateContextsAsync(this.block.Transactions[0], cancellationToken),
                    Times.Once()
                );

                this.handler.Verify(
                    h => h.AddWatchesAsync(It.IsAny<IEnumerable<TransactionWatch<object>>>(), It.IsAny<CancellationToken>()),
                    Times.Never()
                );
            });
        }

        [Fact]
        public Task ExecuteAsync_CreateContextsAsyncReturnNonEmptyList_ShouldCreateWatchesEqualToNumberOfContexts()
        {
            return AsynchronousTesting.WithCancellationTokenAsync(async cancellationToken =>
            {
                // Arrange.
                var ctx1 = new object();
                var ctx2 = new object();

                this.handler.Setup(h => h.CreateContextsAsync(this.block.Transactions[0], It.IsAny<CancellationToken>()))
                            .Returns(Task.FromResult<IEnumerable<object>>(new[] { ctx1, ctx2 }));

                // Act.
                await this.subject.ExecuteAsync(this.block, 0, BlockEventType.Added, cancellationToken);

                // Assert.
                this.handler.Verify(
                    h => h.CreateContextsAsync(this.block.Transactions[0], cancellationToken),
                    Times.Once()
                );

                this.handler.Verify(
                    h => h.AddWatchesAsync(
                        It.Is<IEnumerable<TransactionWatch<object>>>(l => l.Count() == 2 && l.First().Context == ctx1 && l.Last().Context == ctx2),
                        cancellationToken
                    ),
                    Times.Once()
                );
            });
        }

        [Theory]
        [InlineData(BlockEventType.Added)]
        [InlineData(BlockEventType.Removing)]
        public Task ExecuteAsync_ConfirmationUpdateAsyncReturnTrue_ShouldRemoveThatWatch(BlockEventType eventType)
        {
            return AsynchronousTesting.WithCancellationTokenAsync(async cancellationToken =>
            {
                // Arrange.
                var watch1 = new TransactionWatch<object>(null, TestBlock.Regtest0.GetHash(), uint256.One);
                var watch2 = new TransactionWatch<object>(null, TestBlock.Regtest1.GetHash(), uint256.One);
                var watches = new[] { watch1, watch2 };

                var confirmationType = FakeConfirmationWatcher.GetConfirmationType(eventType);

                this.handler.Setup(h => h.GetCurrentWatchesAsync(It.IsAny<CancellationToken>()))
                            .Returns(Task.FromResult<IEnumerable<TransactionWatch<object>>>(watches));

                this.handler.Setup(h => h.ConfirmationUpdateAsync(watch1, 2, confirmationType, It.IsAny<CancellationToken>()))
                            .Returns(Task.FromResult(false));
                this.handler.Setup(h => h.ConfirmationUpdateAsync(watch2, 1, confirmationType, It.IsAny<CancellationToken>()))
                            .Returns(Task.FromResult(true));

                this.blocks.Setup(b => b.GetAsync(TestBlock.Regtest0.GetHash(), It.IsAny<CancellationToken>()))
                           .Returns(Task.FromResult((TestBlock.Regtest0, 0)));
                this.blocks.Setup(b => b.GetAsync(TestBlock.Regtest1.GetHash(), It.IsAny<CancellationToken>()))
                           .Returns(Task.FromResult((TestBlock.Regtest1, 1)));

                // Act.
                await this.subject.ExecuteAsync(TestBlock.Regtest1, 1, eventType, cancellationToken);

                // Assert.
                this.handler.Verify(
                    h => h.ConfirmationUpdateAsync(
                        watch1,
                        2,
                        confirmationType,
                        CancellationToken.None
                    ),
                    Times.Once()
                );

                this.handler.Verify(
                    h => h.ConfirmationUpdateAsync(
                        watch2,
                        1,
                        confirmationType,
                        CancellationToken.None
                    ),
                    Times.Once()
                );

                this.handler.Verify(
                    h => h.RemoveCompletedWatchesAsync(
                        new[] { watch2 },
                        CancellationToken.None
                    ),
                    Times.Once()
                );
            });
        }
    }
}
