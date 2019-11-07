using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using NSubstitute;
using Xunit;
using Ztm.Data.Entity.Testing;
using Ztm.Zcoin.NBitcoin;
using Ztm.Zcoin.Synchronization;

namespace Ztm.WebApi.Tests
{
    using WatchRepository = ITransactionConfirmationWatchRepository<TransactionConfirmationCallbackResult>;

    public sealed class TransactionConfirmationWatcherTests : IDisposable
    {
        readonly TransactionConfirmationWatcher subject;

        readonly TestMainDatabaseFactory databaseFactory;
        readonly ICallbackRepository callbackRepository;
        readonly WatchRepository watchRepository;
        readonly IBlocksStorage blockStorage;
        readonly ICallbackExecuter callbackExecuter;

        volatile bool initialized = false;

        public TransactionConfirmationWatcherTests()
        {
            this.databaseFactory = new TestMainDatabaseFactory();
            this.callbackRepository = new SqlCallbackRepository(databaseFactory);
            this.watchRepository = new SqlTransactionConfirmationWatchRepository
                <TransactionConfirmationCallbackResult>(databaseFactory);

            this.blockStorage = Substitute.For<IBlocksStorage>();
            this.blockStorage
                .GetAsync(Arg.Any<uint256>(), Arg.Any<CancellationToken>())
                .Returns(info => mockedBlocks[info.ArgAt<uint256>(0)].ToValueTuple());

            this.callbackExecuter = Substitute.For<ICallbackExecuter>();

            this.subject = new TransactionConfirmationWatcher
            (
                this.callbackRepository,
                this.watchRepository,
                this.blockStorage,
                this.callbackExecuter
            );
        }

        public async void Dispose()
        {
            this.databaseFactory.Dispose();

            if (initialized)
            {
                await this.subject.StopAsync(CancellationToken.None);
            }
        }


        async Task Initialize(CancellationToken cancellationToken)
        {
            await this.subject.StartAsync(cancellationToken);
            initialized = true;
        }

        [Fact]
        public void Construct_WithValidArgs_ShouldSuccess()
        {
            new TransactionConfirmationWatcher
            (
                this.callbackRepository,
                this.watchRepository,
                this.blockStorage,
                this.callbackExecuter
            );
        }

        [Fact]
        public void Construct_WithNullArg_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>
            (
                "callbackRepository",
                () => new TransactionConfirmationWatcher(null, this.watchRepository, this.blockStorage, this.callbackExecuter)
            );

            Assert.Throws<ArgumentNullException>
            (
                "watchRepository",
                () => new TransactionConfirmationWatcher(this.callbackRepository, null, this.blockStorage, this.callbackExecuter)
            );

            Assert.Throws<ArgumentNullException>
            (
                "blocks",
                () => new TransactionConfirmationWatcher(this.callbackRepository, this.watchRepository, null, this.callbackExecuter)
            );

            Assert.Throws<ArgumentNullException>
            (
                "callbackExecuter",
                () => new TransactionConfirmationWatcher(this.callbackRepository, this.watchRepository, this.blockStorage, null)
            );
        }

        [Fact]
        public async Task AddTransactionAsync_AndNoEventPush_ShouldTimeout()
        {
            // Arrange.
            await this.Initialize(CancellationToken.None);
            var builder = new TransactionConfirmationWatcherHandlerTests.WatchArgsBuilder();
            builder.timeout = TimeSpan.FromSeconds(1);

            // Act.
            await builder.Call(this.subject.AddTransactionAsync);
            Thread.Sleep(TimeSpan.FromSeconds(2));

            // Assert.
            _ = this.callbackExecuter
                .Received(1)
                .Execute
                (
                    Arg.Any<Uri>(),
                    Arg.Is<TransactionConfirmationCallbackResult>(r => r.Status == CallbackResult.StatusError)
                );
        }

        [Fact]
        public async Task AddTransactionAsync_AndPushBlockWhichContainTransaction_TimerShouldBeStopped()
        {
            // Arrange.
            var (block, _) = GenerateBlock();
            block.AddTransaction(Transaction.Create(ZcoinNetworks.Instance.Regtest));

            var builder = new TransactionConfirmationWatcherHandlerTests.WatchArgsBuilder();
            builder.timeout = TimeSpan.FromSeconds(1);
            builder.transaction = block.Transactions[0].GetHash();

            await builder.Call(this.subject.AddTransactionAsync);

            // Act.
            await this.subject.BlockAddedAsync(block, 1, CancellationToken.None);
            Thread.Sleep(TimeSpan.FromSeconds(1));

            // Assert.
            this.callbackExecuter.Received(0);
        }

        [Fact]
        public async Task AddTransactionAsync_AndPushUntilRequiredConfirmation_ShouldCallSuccess()
        {
            // Arrange.
            var (block, _) = GenerateBlock();
            block.AddTransaction(Transaction.Create(ZcoinNetworks.Instance.Regtest));

            var builder = new TransactionConfirmationWatcherHandlerTests.WatchArgsBuilder();
            builder.confirmation = 10;
            builder.timeout = TimeSpan.FromSeconds(1);
            builder.transaction = block.Transactions[0].GetHash();

            await builder.Call(this.subject.AddTransactionAsync);

            // Act.
            await this.subject.BlockAddedAsync(block, 1, CancellationToken.None);
            for (var i = 0; i < 9; i++)
            {
                int height;
                (block, height) = GenerateBlock();

                await this.subject.BlockAddedAsync(block, height, CancellationToken.None);
            }
            Thread.Sleep(TimeSpan.FromMilliseconds(1));

            // Assert.
            _ = this.callbackExecuter
                .Received(1)
                .Execute
                (
                    Arg.Any<Uri>(),
                    Arg.Is<TransactionConfirmationCallbackResult>
                    (
                        r => r.Status == CallbackResult.StatusSuccess
                    )
                );
        }

        [Fact]
        public async Task AddTransactionAsync_AndBlocksAreRemove_TimerShouldBeResume()
        {
            // Arrange.
            var (block, _) = GenerateBlock();
            block.AddTransaction(Transaction.Create(ZcoinNetworks.Instance.Regtest));

            var builder = new TransactionConfirmationWatcherHandlerTests.WatchArgsBuilder();
            builder.timeout = TimeSpan.FromMilliseconds(500);
            builder.transaction = block.Transactions[0].GetHash();

            await builder.Call(this.subject.AddTransactionAsync);

            // Act.
            await this.subject.BlockAddedAsync(block, 1, CancellationToken.None);
            await this.subject.BlockRemovingAsync(block, 1, CancellationToken.None);
            Thread.Sleep(TimeSpan.FromMilliseconds(1000));

            // Assert.
            _ = this.callbackExecuter
                .Received(1)
                .Execute
                (
                    Arg.Any<Uri>(),
                    Arg.Is<TransactionConfirmationCallbackResult>
                    (
                        r => r.Status == CallbackResult.StatusError
                    )
                );
        }

        Dictionary<uint256, Tuple<Block, int>> mockedBlocks = new Dictionary<uint256, Tuple<Block, int>>();
        Tuple<Block, int> generatedBlock = Tuple.Create(ZcoinNetworks.Instance.Regtest.GetGenesis(), 0);

        (Block, int) GenerateBlock()
        {
            generatedBlock = Tuple.Create
            (
                generatedBlock.Item1.CreateNextBlockWithCoinbase
                (
                    BitcoinAddress.Create("TDk19wPKYq91i18qmY6U9FeTdTxwPeSveo", ZcoinNetworks.Instance.Regtest),
                    generatedBlock.Item2 + 1
                ),
                generatedBlock.Item2 + 1
            );

            mockedBlocks[generatedBlock.Item1.GetHash()] = generatedBlock;
            return generatedBlock.ToValueTuple();
        }
    }
}