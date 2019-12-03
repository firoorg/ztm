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
using Ztm.Zcoin.NBitcoin;

namespace Ztm.Zcoin.Synchronization.Tests
{
    public sealed class BlocksSynchronizerTests
    {
        readonly Mock<IBackgroundServiceExceptionHandler> exceptionHandler;
        readonly ILogger<BlocksSynchronizer> logger;
        readonly IBlocksRetriever retriever;
        readonly IBlocksStorage storage;
        readonly IBlockListener listener1, listener2;
        readonly BlocksSynchronizer subject;

        public BlocksSynchronizerTests()
        {
            this.exceptionHandler = new Mock<IBackgroundServiceExceptionHandler>();
            this.logger = Substitute.For<ILogger<BlocksSynchronizer>>();
            this.retriever = Substitute.For<IBlocksRetriever>();
            this.storage = Substitute.For<IBlocksStorage>();
            this.listener1 = Substitute.For<IBlockListener>();
            this.listener2 = Substitute.For<IBlockListener>();
            this.subject = new BlocksSynchronizer(
                this.exceptionHandler.Object,
                ZcoinNetworks.Instance.Regtest,
                this.logger,
                this.retriever,
                this.storage,
                new[] { this.listener1, this.listener2 }
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
                    this.storage,
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
                    this.storage,
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
                    this.storage,
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
                    this.storage,
                    null
                )
            );
        }

        [Fact]
        public async Task GetBlockHintAsync_NoLocalBlocks_ShouldReturnZero()
        {
            // Arrange.
            var subject = this.subject as IBlocksRetrieverHandler;

            this.storage.GetLastAsync(Arg.Any<CancellationToken>()).Returns((null, 0));

            // Act.
            var height = await subject.GetBlockHintAsync(CancellationToken.None);

            // Assert.
            Assert.Equal(0, height);
        }

        [Fact]
        public async Task GetBlockHintAsync_HaveLocalBlocks_ShouldReturnNextHeight()
        {
            // Arrange.
            var subject = this.subject as IBlocksRetrieverHandler;
            var block = ZcoinNetworks.Instance.Regtest.GetGenesis();

            this.storage.GetLastAsync(Arg.Any<CancellationToken>()).Returns((block, 0));

            // Act.
            var height = await subject.GetBlockHintAsync(CancellationToken.None);

            // Assert.
            Assert.Equal(1, height);
        }

        [Fact]
        public async Task ProcessBlockAsync_NoLocalBlocksWithNonZeroHeight_ShouldReturnZero()
        {
            // Arrange.
            var subject = this.subject as IBlocksRetrieverHandler;
            var block = ZcoinNetworks.Instance.Regtest.GetGenesis();

            this.storage.GetLastAsync(Arg.Any<CancellationToken>()).Returns((null, 0));

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

            this.storage.GetLastAsync(Arg.Any<CancellationToken>()).Returns((null, 0));

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

            this.storage.GetLastAsync(Arg.Any<CancellationToken>()).Returns((null, 0));
            this.storage.AddAsync(block, 0, Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

            // Act.
            using (var cancellationSource = new CancellationTokenSource())
            {
                var height = await subject.ProcessBlockAsync(block, 0, cancellationSource.Token);

                // Assert.
                _ = this.storage.Received(1).AddAsync(block, 0, cancellationSource.Token);
                _ = this.listener1.Received(1).BlockAddedAsync(block, 0, CancellationToken.None);
                _ = this.listener2.Received(1).BlockAddedAsync(block, 0, CancellationToken.None);

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

            this.storage.GetLastAsync(Arg.Any<CancellationToken>()).Returns((genesis, 0));

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

            this.storage.GetLastAsync(Arg.Any<CancellationToken>()).Returns((genesis, 0));
            this.storage.RemoveLastAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

            // Act.
            var height = await subject.ProcessBlockAsync(block2, 1, CancellationToken.None);

            // Assert.
            _ = this.storage.Received(1).RemoveLastAsync(CancellationToken.None);
            _ = this.listener1.Received(1).BlockRemovingAsync(genesis, 0, CancellationToken.None);
            _ = this.listener2.Received(1).BlockRemovingAsync(genesis, 0, CancellationToken.None);

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

            this.storage.GetLastAsync(Arg.Any<CancellationToken>()).Returns((genesis, 0));
            this.storage.AddAsync(block1, 1, Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

            // Act.
            using (var cancellationSource = new CancellationTokenSource())
            {
                var height = await subject.ProcessBlockAsync(block1, 1, cancellationSource.Token);

                // Assert.
                _ = this.storage.Received(1).AddAsync(block1, 1, cancellationSource.Token);
                _ = this.listener1.Received(1).BlockAddedAsync(block1, 1, CancellationToken.None);
                _ = this.listener2.Received(1).BlockAddedAsync(block1, 1, CancellationToken.None);

                Assert.Equal(2, height);
            }
        }
    }
}
