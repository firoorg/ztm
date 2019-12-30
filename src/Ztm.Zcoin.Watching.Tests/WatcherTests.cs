using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NBitcoin;
using Xunit;
using Ztm.Testing;

namespace Ztm.Zcoin.Watching.Tests
{
    public sealed class WatcherTests
    {
        readonly Mock<IWatcherHandler<object, Watch<object>>> handler;
        readonly FakeWatcher subject;

        public WatcherTests()
        {
            this.handler = new Mock<IWatcherHandler<object, Watch<object>>>();
            this.subject = new FakeWatcher(this.handler.Object);
        }

        [Fact]
        public void Constructor_WithNullHandler_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>("handler", () => new FakeWatcher(null));
        }

        [Fact]
        public void ExecuteAsync_WithNullBlock_ShouldThrow()
        {
            this.subject.Invoking(s => s.ExecuteAsync(null, 0, BlockEventType.Added, CancellationToken.None))
                        .Should().ThrowExactly<ArgumentNullException>()
                        .And.ParamName.Should().Be("block");
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-2)]
        public void ExecuteAsync_WithNegativeHeight_ShouldThrow(int height)
        {
            this.subject.Invoking(s => s.ExecuteAsync(TestBlock.Regtest0, height, BlockEventType.Added, CancellationToken.None))
                        .Should().ThrowExactly<ArgumentOutOfRangeException>()
                        .And.ParamName.Should().Be("height");
        }

        [Fact]
        public Task ExecuteAsync_CreateWatchesAsyncReturnEmptyList_ShouldNotInvokeAddWatchesAsync()
        {
            return AsynchronousTesting.WithCancellationTokenAsync(async cancellationToken =>
            {
                // Arrange.
                this.subject.StubbedCreateWatchesAsync.Setup(f => f(It.IsAny<Block>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                                                      .Returns(Task.FromResult(Enumerable.Empty<Watch<object>>()));

                // Act.
                await this.subject.ExecuteAsync(TestBlock.Regtest0, 0, BlockEventType.Added, cancellationToken);

                // Assert.
                this.subject.StubbedCreateWatchesAsync.Verify(
                    f => f(TestBlock.Regtest0, 0, cancellationToken),
                    Times.Once()
                );

                this.handler.Verify(
                    h => h.AddWatchesAsync(It.IsAny<IEnumerable<Watch<object>>>(), It.IsAny<CancellationToken>()),
                    Times.Never()
                );
            });
        }

        [Fact]
        public Task ExecuteAsync_CreateWatchesAsyncReturnNonEmptyList_ShouldInvokeAddWatchesAsync()
        {
            return AsynchronousTesting.WithCancellationTokenAsync(async cancellationToken =>
            {
                // Arrange.
                var watch = new Watch<object>(null, TestBlock.Regtest0.GetHash());

                this.subject.StubbedCreateWatchesAsync.Setup(f => f(TestBlock.Regtest0, 0, cancellationToken))
                                                      .Returns(Task.FromResult<IEnumerable<Watch<object>>>(new[] { watch }));

                // Act.
                await this.subject.ExecuteAsync(TestBlock.Regtest0, 0, BlockEventType.Added, cancellationToken);

                // Assert.
                this.subject.StubbedCreateWatchesAsync.Verify(
                    f => f(TestBlock.Regtest0, 0, cancellationToken),
                    Times.Once()
                );

                this.handler.Verify(
                    h => h.AddWatchesAsync(It.Is<IEnumerable<Watch<object>>>(l => l.Single() == watch), cancellationToken),
                    Times.Once()
                );
            });
        }

        [Theory]
        [InlineData(BlockEventType.Added)]
        [InlineData(BlockEventType.Removing)]
        public Task ExecuteAsync_GetWatchesAsyncReturnEmptyList_ShouldDoNothing(BlockEventType eventType)
        {
            return AsynchronousTesting.WithCancellationTokenAsync(async cancellationToken =>
            {
                // Arrange.
                this.subject.StubbedGetWatchesAsync.Setup(f => f(TestBlock.Regtest0, 0, cancellationToken))
                                                   .Returns(Task.FromResult(Enumerable.Empty<Watch<object>>()));

                // Act.
                await this.subject.ExecuteAsync(TestBlock.Regtest0, 0, eventType, cancellationToken);

                // Assert.
                this.subject.StubbedGetWatchesAsync.Verify(
                    f => f(
                        TestBlock.Regtest0,
                        0,
                        cancellationToken
                    ),
                    Times.Once()
                );

                this.subject.StubbedExecuteWatchesAsync.Verify(
                    f => f(
                        It.IsAny<IEnumerable<Watch<object>>>(),
                        It.IsAny<Block>(),
                        It.IsAny<int>(),
                        It.IsAny<BlockEventType>(),
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Never()
                );

                this.handler.Verify(
                    h => h.RemoveCompletedWatchesAsync(
                        It.IsAny<IEnumerable<Watch<object>>>(),
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Never()
                );

                this.handler.Verify(
                    h => h.RemoveUncompletedWatchesAsync(
                        It.IsAny<uint256>(),
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Never()
                );
            });
        }

        [Fact]
        public Task ExecuteAsync_BlockAddedAndExecuteWatchesAsyncReturnEmptyList_ShouldNotRemoveAnyWatches()
        {
            return AsynchronousTesting.WithCancellationTokenAsync(async cancellationToken =>
            {
                // Arrange.
                var watch = new Watch<object>(null, TestBlock.Regtest0.GetHash());
                var watches = new[] { watch };

                this.subject.StubbedGetWatchesAsync.Setup(f => f(TestBlock.Regtest0, 0, It.IsAny<CancellationToken>()))
                                                   .Returns(Task.FromResult<IEnumerable<Watch<object>>>(watches));

                this.subject.StubbedExecuteWatchesAsync.Setup(f => f(watches, TestBlock.Regtest0, 0, BlockEventType.Added, It.IsAny<CancellationToken>()))
                                                       .Returns(Task.FromResult<ISet<Watch<object>>>(new HashSet<Watch<object>>()));

                // Act.
                await this.subject.ExecuteAsync(TestBlock.Regtest0, 0, BlockEventType.Added, cancellationToken);

                // Assert.
                this.subject.StubbedGetWatchesAsync.Verify(
                    f => f(
                        TestBlock.Regtest0,
                        0,
                        cancellationToken
                    ),
                    Times.Once()
                );

                this.subject.StubbedExecuteWatchesAsync.Verify(
                    f => f(
                        watches,
                        TestBlock.Regtest0,
                        0,
                        BlockEventType.Added,
                        cancellationToken
                    ),
                    Times.Once()
                );

                this.handler.Verify(
                    h => h.RemoveCompletedWatchesAsync(
                        It.IsAny<IEnumerable<Watch<object>>>(),
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Never()
                );

                this.handler.Verify(
                    h => h.RemoveUncompletedWatchesAsync(
                        It.IsAny<uint256>(),
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Never()
                );
            });
        }

        [Fact]
        public Task ExecuteAsync_BlockAddedAndExecuteWatchesAsyncReturnNonEmptyList_ShouldRemoveReturnedWatches()
        {
            return AsynchronousTesting.WithCancellationTokenAsync(async cancellationToken =>
            {
                // Arrange.
                var watch1 = new Watch<object>(null, TestBlock.Regtest0.GetHash());
                var watch2 = new Watch<object>(null, TestBlock.Regtest0.GetHash());
                var watches = new[] { watch1, watch2 };
                var removes = new HashSet<Watch<object>>() { watch2 };

                this.subject.StubbedGetWatchesAsync.Setup(f => f(TestBlock.Regtest0, 0, It.IsAny<CancellationToken>()))
                                                   .Returns(Task.FromResult<IEnumerable<Watch<object>>>(watches));

                this.subject.StubbedExecuteWatchesAsync.Setup(f => f(watches, TestBlock.Regtest0, 0, BlockEventType.Added, It.IsAny<CancellationToken>()))
                                                       .Returns(Task.FromResult<ISet<Watch<object>>>(removes));

                // Act.
                await this.subject.ExecuteAsync(TestBlock.Regtest0, 0, BlockEventType.Added, cancellationToken);

                // Assert.
                this.subject.StubbedGetWatchesAsync.Verify(
                    f => f(TestBlock.Regtest0, 0, cancellationToken),
                    Times.Once()
                );

                this.subject.StubbedExecuteWatchesAsync.Verify(
                    f => f(watches, TestBlock.Regtest0, 0, BlockEventType.Added, cancellationToken),
                    Times.Once()
                );

                this.handler.Verify(
                    h => h.RemoveCompletedWatchesAsync(removes, CancellationToken.None),
                    Times.Once()
                );

                this.handler.Verify(
                    h => h.RemoveUncompletedWatchesAsync(It.IsAny<uint256>(), It.IsAny<CancellationToken>()),
                    Times.Never()
                );
            });
        }

        [Fact]
        public Task ExecuteAsync_BlockRemovingAndExecuteWatchesAsyncReturnEmptyList_ShouldRemoveWatchesForThatBlock()
        {
            return AsynchronousTesting.WithCancellationTokenAsync(async cancellationToken =>
            {
                // Arrange.
                var watch1 = new Watch<object>(null, TestBlock.Regtest0.GetHash());
                var watch2 = new Watch<object>(null, TestBlock.Regtest1.GetHash());
                var watches = new[] { watch1, watch2 };

                this.subject.StubbedGetWatchesAsync.Setup(f => f(TestBlock.Regtest1, 1, It.IsAny<CancellationToken>()))
                                                   .Returns(Task.FromResult<IEnumerable<Watch<object>>>(watches));

                this.subject.StubbedExecuteWatchesAsync.Setup(f => f(watches, TestBlock.Regtest1, 1, BlockEventType.Removing, It.IsAny<CancellationToken>()))
                                                       .Returns(Task.FromResult<ISet<Watch<object>>>(new HashSet<Watch<object>>()));

                // Act.
                await this.subject.ExecuteAsync(TestBlock.Regtest1, 1, BlockEventType.Removing, cancellationToken);

                // Assert.
                this.subject.StubbedGetWatchesAsync.Verify(
                    f => f(
                        TestBlock.Regtest1,
                        1,
                        cancellationToken
                    ),
                    Times.Once()
                );

                this.subject.StubbedExecuteWatchesAsync.Verify(
                    f => f(
                        watches,
                        TestBlock.Regtest1,
                        1,
                        BlockEventType.Removing,
                        cancellationToken
                    ),
                    Times.Once()
                );

                this.handler.Verify(
                    h => h.RemoveCompletedWatchesAsync(
                        It.IsAny<IEnumerable<Watch<object>>>(),
                        It.IsAny<CancellationToken>()
                    ),
                    Times.Never()
                );

                this.handler.Verify(
                    h => h.RemoveUncompletedWatchesAsync(
                        TestBlock.Regtest1.GetHash(),
                        CancellationToken.None
                    ),
                    Times.Once()
                );
            });
        }

        [Fact]
        public Task ExecuteAsync_BlockRemovingAndExecuteWatchesAsyncReturnNonEmptyList_ShouldRemoveReturnedWatchesAndForThatBlock()
        {
            return AsynchronousTesting.WithCancellationTokenAsync(async cancellationToken =>
            {
                // Arrange.
                var watch1 = new Watch<object>(null, TestBlock.Regtest0.GetHash());
                var watch2 = new Watch<object>(null, TestBlock.Regtest1.GetHash());
                var watch3 = new Watch<object>(null, TestBlock.Regtest1.GetHash());
                var watches = new[] { watch1, watch2, watch3 };
                var removes = new HashSet<Watch<object>> { watch2 };

                this.subject.StubbedGetWatchesAsync.Setup(f => f(TestBlock.Regtest1, 1, It.IsAny<CancellationToken>()))
                                                   .Returns(Task.FromResult<IEnumerable<Watch<object>>>(watches));

                this.subject.StubbedExecuteWatchesAsync.Setup(f => f(watches, TestBlock.Regtest1, 1, BlockEventType.Removing, It.IsAny<CancellationToken>()))
                                                       .Returns(Task.FromResult<ISet<Watch<object>>>(removes));

                // Act.
                await this.subject.ExecuteAsync(TestBlock.Regtest1, 1, BlockEventType.Removing, cancellationToken);

                // Assert.
                this.subject.StubbedGetWatchesAsync.Verify(
                    f => f(TestBlock.Regtest1, 1, cancellationToken),
                    Times.Once()
                );

                this.subject.StubbedExecuteWatchesAsync.Verify(
                    f => f(watches, TestBlock.Regtest1, 1, BlockEventType.Removing, cancellationToken),
                    Times.Once()
                );

                this.handler.Verify(
                    h => h.RemoveCompletedWatchesAsync(removes, CancellationToken.None),
                    Times.Once()
                );

                this.handler.Verify(
                    h => h.RemoveUncompletedWatchesAsync(TestBlock.Regtest1.GetHash(), CancellationToken.None),
                    Times.Once()
                );
            });
        }
    }
}
