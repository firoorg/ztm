using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NBitcoin;
using NSubstitute;
using Xunit;
using Ztm.Zcoin.NBitcoin;

namespace Ztm.Zcoin.Synchronization.Tests
{
    public sealed class BlocksSynchronizerTests : IDisposable
    {
        readonly IConfiguration config;
        readonly ILogger<BlocksSynchronizer> logger;
        readonly IBlocksRetriever retriever;
        readonly IBlocksStorage storage;
        readonly IBlockListener listener1, listener2;
        readonly BlocksSynchronizer subject;

        public BlocksSynchronizerTests()
        {
            var builder = new ConfigurationBuilder();

            builder.AddInMemoryCollection(new Dictionary<string, string>()
            {
                {"Zcoin:Network:Type", "Regtest"}
            });

            this.config = builder.Build();
            this.logger = Substitute.For<ILogger<BlocksSynchronizer>>();
            this.retriever = Substitute.For<IBlocksRetriever>();
            this.storage = Substitute.For<IBlocksStorage>();
            this.listener1 = Substitute.For<IBlockListener>();
            this.listener2 = Substitute.For<IBlockListener>();
            this.subject = new BlocksSynchronizer(
                this.config,
                this.logger,
                this.retriever,
                this.storage,
                this.listener1,
                this.listener2
            );
        }

        public void Dispose()
        {
            this.subject.Dispose();
        }

        [Fact]
        public void Constructor_PassNullForConfig_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "config",
                () => new BlocksSynchronizer(null, this.logger, this.retriever, this.storage)
            );
        }

        [Fact]
        public void Constructor_PassNullForLogger_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "logger",
                () => new BlocksSynchronizer(this.config, null, this.retriever, this.storage)
            );
        }

        [Fact]
        public void Constructor_PassNullForRetriever_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "retriever",
                () => new BlocksSynchronizer(this.config, this.logger, null, this.storage)
            );
        }

        [Fact]
        public void Constructor_PassNullForStorage_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "storage",
                () => new BlocksSynchronizer(this.config, this.logger, this.retriever, null)
            );
        }

        [Fact]
        public void Constructor_PassNullForListeners_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "listeners",
                () => new BlocksSynchronizer(this.config, this.logger, this.retriever, this.storage, null)
            );
        }

        [Fact]
        public void Constructor_WhenSuccess_ShouldAssignNameToNonNull()
        {
            Assert.NotNull(this.subject.Name);
        }

        [Fact]
        public void Dispose_WhenSuccess_ShouldDisposeRetriever()
        {
            // Act.
            this.subject.Dispose();

            // Assert.
            this.retriever.Received(1).Dispose();
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
            var block = (ZcoinBlock)ZcoinNetworks.Instance.Regtest.GetGenesis();

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
            var block = (ZcoinBlock)ZcoinNetworks.Instance.Regtest.GetGenesis();

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
            var block = ZcoinBlock.CreateBlock(ZcoinNetworks.Instance.Regtest);

            this.storage.GetLastAsync(Arg.Any<CancellationToken>()).Returns((null, 0));

            // Act.
            await Assert.ThrowsAsync<ArgumentException>(
                "block",
                () => subject.ProcessBlockAsync(block, 0, CancellationToken.None)
            );
        }

        [Fact]
        public async Task ProcessBlockAsync_NoLocalBlocksWithGenesisBlock_ShouldAddToStorageAndRaiseEvent()
        {
            // Arrange.
            var subject = this.subject as IBlocksRetrieverHandler;
            var block = (ZcoinBlock)ZcoinNetworks.Instance.Regtest.GetGenesis();
            var blockAdded = Substitute.For<EventHandler<BlockEventArgs>>();

            this.storage.GetLastAsync(Arg.Any<CancellationToken>()).Returns((null, 0));
            this.storage.AddAsync(block, 0, Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

            this.subject.BlockAdded += blockAdded;

            // Act.
            var height = await subject.ProcessBlockAsync(block, 0, CancellationToken.None);

            // Assert.
            _ = this.storage.Received(1).AddAsync(block, 0, Arg.Any<CancellationToken>());
            _ = this.listener1.Received(1).BlockAddedAsync(block, 0, Arg.Any<CancellationToken>());
            _ = this.listener2.Received(1).BlockAddedAsync(block, 0, Arg.Any<CancellationToken>());
            blockAdded.Received(1).Invoke(this.subject, Arg.Is<BlockEventArgs>(e => e.Block == block && e.Height == 0));

            Assert.Equal(1, height);
        }

        [Fact]
        public async Task ProcessBlockAsync_HaveLocalBlocksWithHeightIsNotNextBlock_ShouldReturnNextLocalHeight()
        {
            // Arrange.
            var subject = this.subject as IBlocksRetrieverHandler;
            var genesis = (ZcoinBlock)ZcoinNetworks.Instance.Regtest.GetGenesis();
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
        public async Task ProcessBlockAsync_HaveLocalBlocksWithNextHeightButPreviousHashIsNotLocal_ShouldDiscardLocalAndRaiseEventThenReturnLocalHeight()
        {
            // Arrange.
            var subject = this.subject as IBlocksRetrieverHandler;
            var genesis = (ZcoinBlock)ZcoinNetworks.Instance.Regtest.GetGenesis();
            var block1 = genesis.CreateNextBlockWithCoinbase(
                BitcoinAddress.Create("THMdcCZXJvUGMHo4BVumsPvPQbzr87Wah7", ZcoinNetworks.Instance.Regtest),
                1
            );
            var block2 = block1.CreateNextBlockWithCoinbase(
                BitcoinAddress.Create("THMdcCZXJvUGMHo4BVumsPvPQbzr87Wah7", ZcoinNetworks.Instance.Regtest),
                2
            );
            var blockRemoved = Substitute.For<EventHandler<BlockEventArgs>>();

            this.storage.GetLastAsync(Arg.Any<CancellationToken>()).Returns((genesis, 0));
            this.storage.RemoveLastAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

            this.subject.BlockRemoved += blockRemoved;

            // Act.
            var height = await subject.ProcessBlockAsync(block2, 1, CancellationToken.None);

            // Assert.
            _ = this.storage.Received(1).RemoveLastAsync(Arg.Any<CancellationToken>());
            _ = this.listener1.Received(1).BlockRemovedAsync(genesis, 0, Arg.Any<CancellationToken>());
            _ = this.listener2.Received(1).BlockRemovedAsync(genesis, 0, Arg.Any<CancellationToken>());
            blockRemoved.Received(1).Invoke(this.subject, Arg.Is<BlockEventArgs>(e => e.Block == genesis && e.Height == 0));

            Assert.Equal(0, height);
        }

        [Fact]
        public async Task ProcessBlockAsync_HaveLocalBlocksWithNextBlock_ShouldAddToStorageAndRaiseEvent()
        {
            // Arrange.
            var subject = this.subject as IBlocksRetrieverHandler;
            var genesis = (ZcoinBlock)ZcoinNetworks.Instance.Regtest.GetGenesis();
            var block1 = genesis.CreateNextBlockWithCoinbase(
                BitcoinAddress.Create("THMdcCZXJvUGMHo4BVumsPvPQbzr87Wah7", ZcoinNetworks.Instance.Regtest),
                1
            );
            var blockAdded = Substitute.For<EventHandler<BlockEventArgs>>();

            this.storage.GetLastAsync(Arg.Any<CancellationToken>()).Returns((genesis, 0));
            this.storage.AddAsync(block1, 1, Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

            this.subject.BlockAdded += blockAdded;

            // Act.
            var height = await subject.ProcessBlockAsync(block1, 1, CancellationToken.None);

            // Assert.
            _ = this.storage.Received(1).AddAsync(block1, 1, Arg.Any<CancellationToken>());
            _ = this.listener1.Received(1).BlockAddedAsync(block1, 1, Arg.Any<CancellationToken>());
            _ = this.listener2.Received(1).BlockAddedAsync(block1, 1, Arg.Any<CancellationToken>());
            blockAdded.Received(1).Invoke(this.subject, Arg.Is<BlockEventArgs>(e => e.Block == block1 && e.Height == 1));

            Assert.Equal(2, height);
        }

        [Fact]
        public async Task StopAsync_WhenInvoke_ShouldCallStopAsync()
        {
            // Arrange.
            var subject = this.subject as IBlocksRetrieverHandler;

            this.retriever.StartAsync(this.subject, Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
            this.retriever.StopAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

            await this.subject.StartAsync(CancellationToken.None);

            // Act.
            await subject.StopAsync(null, CancellationToken.None);
            await Task.Delay(1000); // 1 second should be enough for StopAsync to complete.

            // Assert.
            Assert.False(this.subject.IsRunning);
            Assert.Null(this.subject.Exception);

            _ = this.retriever.Received(1).StopAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task StopAsync_PassNonNullForException_ShouldAssignToException()
        {
            // Arrange.
            var subject = this.subject as IBlocksRetrieverHandler;
            var error = new Exception();

            this.retriever.StartAsync(this.subject, Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
            this.retriever.StopAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

            await this.subject.StartAsync(CancellationToken.None);

            // Act.
            await subject.StopAsync(error, CancellationToken.None);
            await Task.Delay(1000);

            // Assert.
            Assert.False(this.subject.IsRunning);
            Assert.Same(error, this.subject.Exception);

            _ = this.retriever.Received(1).StopAsync(Arg.Any<CancellationToken>());
        }
    }
}
