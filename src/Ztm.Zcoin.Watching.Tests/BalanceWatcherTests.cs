using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NBitcoin;
using Xunit;
using Ztm.Testing;
using Ztm.Zcoin.Synchronization;

namespace Ztm.Zcoin.Watching.Tests
{
    public sealed class BalanceWatcherTests
    {
        readonly Mock<IBalanceWatcherHandler<object, int>> handler;
        readonly Mock<IBlocksStorage> blocks;
        readonly BalanceWatcher<object, int> subject;

        public BalanceWatcherTests()
        {
            this.handler = new Mock<IBalanceWatcherHandler<object, int>>();
            this.blocks = new Mock<IBlocksStorage>();
            this.subject = new BalanceWatcher<object, int>(this.handler.Object, this.blocks.Object);
        }

        [Fact]
        public void Constructor_WithNullHandler_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "handler",
                () => new BalanceWatcher<object, int>(null, this.blocks.Object)
            );
        }

        [Fact]
        public Task ExecuteAsync_GetBalanceChangesAsyncReturnEmptyList_ShouldNotCreateAnyWatches()
        {
            return AsynchronousTesting.WithCancellationTokenAsync(async cancellationToken =>
            {
                // Arrange.
                var changes = new Dictionary<BitcoinAddress, BalanceChange<object, int>>();

                this.handler.Setup(h => h.GetBalanceChangesAsync(It.IsNotNull<Transaction>(), It.IsAny<CancellationToken>()))
                            .ReturnsAsync(changes);

                // Act.
                await this.subject.ExecuteAsync(TestBlock.Regtest0, 0, BlockEventType.Added, cancellationToken);

                // Assert.
                this.handler.Verify(
                    h => h.GetBalanceChangesAsync(
                        It.IsIn<Transaction>(TestBlock.Regtest0.Transactions),
                        cancellationToken
                    ),
                    Times.Exactly(TestBlock.Regtest0.Transactions.Count)
                );

                this.handler.Verify(
                    h => h.AddWatchesAsync(
                        It.IsAny<IEnumerable<BalanceWatch<object, int>>>(),
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Never()
                );
            });
        }

        [Fact]
        public Task ExecuteAsync_GetBalanceChangesAsyncReturnNonEmptyList_ShouldCreateWatchesWithSameAmount()
        {
            return AsynchronousTesting.WithCancellationTokenAsync(async cancellationToken =>
            {
                // Arrange.
                var block = TestBlock.Regtest0;
                var ctx1 = new object();
                var ctx2 = new object();
                var changes = new Dictionary<BitcoinAddress, BalanceChange<object, int>>()
                {
                    { TestAddress.Regtest1, new BalanceChange<object, int>(ctx1, 10) },
                    { TestAddress.Regtest2, new BalanceChange<object, int>(ctx2, -10) }
                };

                this.handler.Setup(h => h.GetBalanceChangesAsync(block.Transactions[0], It.IsAny<CancellationToken>()))
                            .ReturnsAsync(changes);

                // Act.
                await this.subject.ExecuteAsync(block, 0, BlockEventType.Added, cancellationToken);

                // Assert.
                this.handler.Verify(
                    h => h.GetBalanceChangesAsync(block.Transactions[0], cancellationToken),
                    Times.Once()
                );

                this.handler.Verify(
                    h => h.AddWatchesAsync(
                        It.Is<IEnumerable<BalanceWatch<object, int>>>(
                            l => l.Count(w => w.Context == ctx1 && w.StartBlock == block.GetHash() && w.Transaction == block.Transactions[0].GetHash() && w.Address == TestAddress.Regtest1 && w.BalanceChange == 10) == 1 &&
                                 l.Count(w => w.Context == ctx2 && w.StartBlock == block.GetHash() && w.Transaction == block.Transactions[0].GetHash() && w.Address == TestAddress.Regtest2 && w.BalanceChange == -10) == 1
                        ),
                        cancellationToken
                    ),
                    Times.Once()
                );
            });
        }

        [Theory]
        [InlineData(BlockEventType.Added)]
        [InlineData(BlockEventType.Removing)]
        public Task ExecuteAsync_GetCurrentWatchesAsyncReturnNonEmptyList_ShouldInvokeConfirmationUpdateAsync(BlockEventType eventType)
        {
            return AsynchronousTesting.WithCancellationTokenAsync(async cancellationToken =>
            {
                // Arrange.
                var confirmationType = FakeConfirmationWatcher.GetConfirmationType(eventType);
                var block0 = TestBlock.Regtest0;
                var block1 = TestBlock.Regtest1;
                var ctx1 = new object();
                var ctx2 = new object();
                var ctx3 = new object();
                var watch1 = new BalanceWatch<object, int>(ctx1, block0.GetHash(), block0.Transactions[0].GetHash(), TestAddress.Regtest1, 10);
                var watch2 = new BalanceWatch<object, int>(ctx2, block1.GetHash(), block1.Transactions[0].GetHash(), TestAddress.Regtest1, -10);
                var watch3 = new BalanceWatch<object, int>(ctx3, block1.GetHash(), block1.Transactions[0].GetHash(), TestAddress.Regtest2, 5);
                var watches = new[] { watch1, watch2, watch3 };

                this.handler.Setup(h => h.GetBalanceChangesAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
                            .ReturnsAsync(new Dictionary<BitcoinAddress, BalanceChange<object, int>>());

                this.handler.Setup(h => h.GetCurrentWatchesAsync(It.IsAny<CancellationToken>()))
                            .ReturnsAsync(watches);

                this.handler.Setup(h => h.ConfirmationUpdateAsync(It.Is<BalanceConfirmation<object, int>>(c => c.Address == TestAddress.Regtest1), 1, confirmationType, It.IsAny<CancellationToken>()))
                            .ReturnsAsync(true);

                this.blocks.Setup(b => b.GetAsync(block0.GetHash(), It.IsAny<CancellationToken>()))
                           .ReturnsAsync((block0, 0));

                this.blocks.Setup(b => b.GetAsync(block1.GetHash(), It.IsAny<CancellationToken>()))
                           .ReturnsAsync((block1, 1));

                // Act.
                await this.subject.ExecuteAsync(block1, 1, eventType, cancellationToken);

                // Assert.
                this.handler.Verify(
                    h => h.GetCurrentWatchesAsync(cancellationToken),
                    Times.Once()
                );

                this.handler.Verify(
                    h => h.ConfirmationUpdateAsync(
                        It.Is<BalanceConfirmation<object, int>>(
                            c => c.Address == TestAddress.Regtest1 &&
                                 c.Changes.Count() == 2 &&
                                 c.Changes.Count(bc => bc.Amount == 10 && bc.Confirmation == 2 && bc.Context == ctx1) == 1 &&
                                 c.Changes.Count(bc => bc.Amount == -10 && bc.Confirmation == 1 && bc.Context == ctx2) == 1
                        ),
                        1,
                        confirmationType,
                        CancellationToken.None
                    ),
                    Times.Once()
                );

                this.handler.Verify(
                    h => h.ConfirmationUpdateAsync(
                        It.Is<BalanceConfirmation<object, int>>(
                            c => c.Address == TestAddress.Regtest2 &&
                                 c.Changes.Count() == 1 &&
                                 c.Changes.Count(bc => bc.Amount == 5 && bc.Confirmation == 1 && bc.Context == ctx3) == 1
                        ),
                        1,
                        confirmationType,
                        CancellationToken.None
                    ),
                    Times.Once()
                );

                this.handler.Verify(
                    h => h.RemoveCompletedWatchesAsync(
                        It.Is<IEnumerable<BalanceWatch<object, int>>>(
                            l => l.Count() == 2 && l.Contains(watch1) && l.Contains(watch2)
                        ),
                        CancellationToken.None
                    ),
                    Times.Once()
                );
            });
        }
    }
}
