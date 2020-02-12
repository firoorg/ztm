using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NBitcoin;
using NSubstitute;
using Xunit;
using Ztm.Hosting;
using Ztm.Testing;
using Ztm.Zcoin.NBitcoin;

namespace Ztm.Zcoin.Synchronization.Tests
{
    public sealed class BlocksSynchronizerTests
    {
        readonly Mock<IBackgroundServiceExceptionHandler> exceptionHandler;
        readonly ILogger<BlocksSynchronizer> logger;
        readonly IBlocksRetriever retriever;
        readonly Mock<IBlocksStorage> storage;
        readonly Mock<IBlockListener> listener1;
        readonly Mock<IBlockListener> listener2;
        readonly BlocksSynchronizer subject;

        public BlocksSynchronizerTests()
        {
            this.exceptionHandler = new Mock<IBackgroundServiceExceptionHandler>();
            this.logger = Substitute.For<ILogger<BlocksSynchronizer>>();
            this.retriever = Substitute.For<IBlocksRetriever>();
            this.storage = new Mock<IBlocksStorage>();
            this.listener1 = new Mock<IBlockListener>();
            this.listener2 = new Mock<IBlockListener>();
            this.subject = new BlocksSynchronizer(
                this.exceptionHandler.Object,
                ZcoinNetworks.Instance.Regtest,
                this.logger,
                this.retriever,
                this.storage.Object,
                new[] { this.listener1.Object, this.listener2.Object }
            );
        }

        [Fact]
        public void Constructor_PassNullForNetwork_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "network",
                () => new BlocksSynchronizer(
                    this.exceptionHandler.Object,
                    null,
                    this.logger,
                    this.retriever,
                    this.storage.Object,
                    Enumerable.Empty<IBlockListener>()
                )
            );
        }

        [Fact]
        public void Constructor_PassNullForLogger_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "logger",
                () => new BlocksSynchronizer(
                    this.exceptionHandler.Object,
                    ZcoinNetworks.Instance.Regtest,
                    null,
                    this.retriever,
                    this.storage.Object,
                    Enumerable.Empty<IBlockListener>()
                )
            );
        }

        [Fact]
        public void Constructor_PassNullForRetriever_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "retriever",
                () => new BlocksSynchronizer(
                    this.exceptionHandler.Object,
                    ZcoinNetworks.Instance.Regtest,
                    this.logger,
                    null,
                    this.storage.Object,
                    Enumerable.Empty<IBlockListener>()
                )
            );
        }

        [Fact]
        public void Constructor_PassNullForStorage_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "storage",
                () => new BlocksSynchronizer(
                    this.exceptionHandler.Object,
                    ZcoinNetworks.Instance.Regtest,
                    this.logger,
                    this.retriever,
                    null,
                    Enumerable.Empty<IBlockListener>()
                )
            );
        }

        [Fact]
        public void Constructor_PassNullForListeners_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "listeners",
                () => new BlocksSynchronizer(
                    this.exceptionHandler.Object,
                    ZcoinNetworks.Instance.Regtest,
                    this.logger,
                    this.retriever,
                    this.storage.Object,
                    null
                )
            );
        }

        [Fact]
        public Task DiscardBlocksAsync_LocalBlockHigherThanStartBlock_ShouldRemoveLatestBlockUntilHeightIsStartBlock()
        {
            return AsynchronousTesting.WithCancellationTokenAsync(async cancellationToken =>
            {
                // Arrange.
                this.storage
                    .SetupSequence(s => s.GetLastAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync((TestBlock.Regtest1, 1))
                    .ReturnsAsync((TestBlock.Regtest0, 0))
                    .ReturnsAsync((null, -1));

                // Act.
                await InvokeDiscardBlocksAsync(0, cancellationToken);

                // Assert.
                this.storage.Verify(
                    s => s.GetLastAsync(cancellationToken),
                    Times.Once());

                this.storage.Verify(
                    s => s.GetLastAsync(CancellationToken.None),
                    Times.Exactly(2));

                this.listener1.Verify(
                    l => l.BlockRemovingAsync(TestBlock.Regtest1, 1, CancellationToken.None),
                    Times.Once());

                this.listener2.Verify(
                    l => l.BlockRemovingAsync(TestBlock.Regtest1, 1, CancellationToken.None),
                    Times.Once());

                this.listener1.Verify(
                    l => l.BlockRemovingAsync(TestBlock.Regtest0, 0, CancellationToken.None),
                    Times.Once());

                this.listener2.Verify(
                    l => l.BlockRemovingAsync(TestBlock.Regtest0, 0, CancellationToken.None),
                    Times.Once());

                this.storage.Verify(
                    s => s.RemoveLastAsync(CancellationToken.None),
                    Times.Exactly(2));
            });
        }

        [Fact]
        public async Task GetStartBlockAsync_NoLocalBlocks_ShouldReturnZero()
        {
            // Arrange.
            var subject = this.subject as IBlocksRetrieverHandler;

            this.storage
                .Setup(s => s.GetLastAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((null, -1));

            // Act.
            var height = await subject.GetStartBlockAsync(CancellationToken.None);

            // Assert.
            Assert.Equal(0, height);
        }

        [Fact]
        public async Task GetStartBlockAsync_HaveLocalBlocks_ShouldReturnNextHeight()
        {
            // Arrange.
            var subject = this.subject as IBlocksRetrieverHandler;
            var block = ZcoinNetworks.Instance.Regtest.GetGenesis();

            this.storage
                .Setup(s => s.GetLastAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((block, 0));

            // Act.
            var height = await subject.GetStartBlockAsync(CancellationToken.None);

            // Assert.
            Assert.Equal(1, height);
        }

        [Fact]
        public async Task ProcessBlockAsync_NoLocalBlocksWithNonZeroHeight_ShouldReturnZero()
        {
            // Arrange.
            var subject = this.subject as IBlocksRetrieverHandler;
            var block = ZcoinNetworks.Instance.Regtest.GetGenesis();

            this.storage
                .Setup(s => s.GetLastAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((null, -1));

            // Act.
            var height = await subject.ProcessBlockAsync(block, 1, CancellationToken.None);

            // Assert.
            Assert.Equal(0, height);
        }

        [Fact]
        public async Task ProcessBlockAsync_NoLocalBlocksWithZeroHeightButNotGenesisBlock_ShouldThrow()
        {
            // Arrange.
            var subject = this.subject as IBlocksRetrieverHandler;
            var block = Block.CreateBlock(ZcoinNetworks.Instance.Regtest);

            this.storage
                .Setup(s => s.GetLastAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((null, -1));

            // Act.
            await Assert.ThrowsAsync<ArgumentException>(
                "block",
                () => subject.ProcessBlockAsync(block, 0, CancellationToken.None)
            );
        }

        [Fact]
        public async Task ProcessBlockAsync_NoLocalBlocksWithGenesisBlock_ShouldAddToStorageAndInvokeListener()
        {
            // Arrange.
            var subject = this.subject as IBlocksRetrieverHandler;
            var block = ZcoinNetworks.Instance.Regtest.GetGenesis();

            this.storage
                .Setup(s => s.GetLastAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((null, -1));

            // Act.
            using (var cancellationSource = new CancellationTokenSource())
            {
                var height = await subject.ProcessBlockAsync(block, 0, cancellationSource.Token);

                // Assert.
                this.storage.Verify(s => s.AddAsync(block, 0, cancellationSource.Token), Times.Once());
                this.listener1.Verify(l => l.BlockAddedAsync(block, 0, CancellationToken.None), Times.Once());
                this.listener2.Verify(l => l.BlockAddedAsync(block, 0, CancellationToken.None), Times.Once());

                Assert.Equal(1, height);
            }
        }

        [Fact]
        public async Task ProcessBlockAsync_HaveLocalBlocksWithHeightIsNotNextBlock_ShouldReturnNextLocalHeight()
        {
            // Arrange.
            var subject = this.subject as IBlocksRetrieverHandler;
            var genesis = ZcoinNetworks.Instance.Regtest.GetGenesis();
            var block1 = genesis.CreateNextBlockWithCoinbase(
                BitcoinAddress.Create("THMdcCZXJvUGMHo4BVumsPvPQbzr87Wah7", ZcoinNetworks.Instance.Regtest),
                1
            );
            var block2 = block1.CreateNextBlockWithCoinbase(
                BitcoinAddress.Create("THMdcCZXJvUGMHo4BVumsPvPQbzr87Wah7", ZcoinNetworks.Instance.Regtest),
                2
            );

            this.storage
                .Setup(s => s.GetLastAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((genesis, 0));

            // Act.
            var height = await subject.ProcessBlockAsync(block2, 2, CancellationToken.None);

            // Assert.
            Assert.Equal(1, height);
        }

        [Fact]
        public async Task ProcessBlockAsync_HaveLocalBlocksWithNextHeightButPreviousHashIsNotLocal_ShouldDiscardLocalAndInvokeListenerThenReturnLocalHeight()
        {
            // Arrange.
            var subject = this.subject as IBlocksRetrieverHandler;
            var genesis = ZcoinNetworks.Instance.Regtest.GetGenesis();
            var block1 = genesis.CreateNextBlockWithCoinbase(
                BitcoinAddress.Create("THMdcCZXJvUGMHo4BVumsPvPQbzr87Wah7", ZcoinNetworks.Instance.Regtest),
                1
            );
            var block2 = block1.CreateNextBlockWithCoinbase(
                BitcoinAddress.Create("THMdcCZXJvUGMHo4BVumsPvPQbzr87Wah7", ZcoinNetworks.Instance.Regtest),
                2
            );

            this.storage
                .Setup(s => s.GetLastAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((genesis, 0));

            // Act.
            var height = await subject.ProcessBlockAsync(block2, 1, CancellationToken.None);

            // Assert.
            this.storage.Verify(s => s.RemoveLastAsync(CancellationToken.None), Times.Once());
            this.listener1.Verify(l => l.BlockRemovingAsync(genesis, 0, CancellationToken.None), Times.Once());
            this.listener2.Verify(l => l.BlockRemovingAsync(genesis, 0, CancellationToken.None), Times.Once());

            Assert.Equal(0, height);
        }

        [Fact]
        public async Task ProcessBlockAsync_HaveLocalBlocksWithNextBlock_ShouldAddToStorageAndInvokeListener()
        {
            // Arrange.
            var subject = this.subject as IBlocksRetrieverHandler;
            var genesis = ZcoinNetworks.Instance.Regtest.GetGenesis();
            var block1 = genesis.CreateNextBlockWithCoinbase(
                BitcoinAddress.Create("THMdcCZXJvUGMHo4BVumsPvPQbzr87Wah7", ZcoinNetworks.Instance.Regtest),
                1
            );

            this.storage
                .Setup(s => s.GetLastAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((genesis, 0));

            // Act.
            using (var cancellationSource = new CancellationTokenSource())
            {
                var height = await subject.ProcessBlockAsync(block1, 1, cancellationSource.Token);

                // Assert.
                this.storage.Verify(s => s.AddAsync(block1, 1, cancellationSource.Token), Times.Once());
                this.listener1.Verify(l => l.BlockAddedAsync(block1, 1, CancellationToken.None), Times.Once());
                this.listener2.Verify(l => l.BlockAddedAsync(block1, 1, CancellationToken.None), Times.Once());

                Assert.Equal(2, height);
            }
        }

        Task InvokeDiscardBlocksAsync(int start, CancellationToken cancellationToken)
        {
            return (this.subject as IBlocksRetrieverHandler).DiscardBlocksAsync(start, cancellationToken);
        }
    }
}
