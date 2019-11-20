using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using NSubstitute;
using Xunit;
using Ztm.Zcoin.NBitcoin;
using Ztm.Zcoin.Synchronization;
using Ztm.Zcoin.Watching;

namespace Ztm.WebApi.Tests
{
    using WatchRepository = ITransactionConfirmationWatchRepository<TransactionConfirmationCallbackResult>;

    public sealed class TransactionConfirmationWatcherTests : IDisposable
    {
        readonly IBlockListener blockListener;
        readonly TransactionConfirmationWatcher subject;
        readonly ITransactionConfirmationWatcherHandler<Guid> handler;

        readonly ICallbackRepository callbackRepository;
        readonly WatchRepository watchRepository;
        readonly IBlocksStorage blockStorage;
        readonly ICallbackExecuter callbackExecuter;

        readonly Uri defaultUrl;

        volatile bool initialized = false;

        public TransactionConfirmationWatcherTests()
        {
            this.callbackRepository = Substitute.For<ICallbackRepository>();
            this.watchRepository = Substitute.For<WatchRepository>();

            this.blockStorage = Substitute.For<IBlocksStorage>();
            this.blockStorage
                .GetAsync(Arg.Any<uint256>(), Arg.Any<CancellationToken>())
                .Returns(info => mockedBlocks[info.ArgAt<uint256>(0)].ToValueTuple());

            this.callbackExecuter = Substitute.For<ICallbackExecuter>();

            this.handler = this.subject = new TransactionConfirmationWatcher
            (
                this.callbackRepository,
                this.watchRepository,
                this.blockStorage,
                this.callbackExecuter
            );
            this.blockListener = this.subject;

            this.defaultUrl = new Uri("http://zcoin.io");

            MockCallbackRepository();
            MockWatchRepository();
        }

        public async void Dispose()
        {
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
        public async Task AddTransactionAsync_WithNullArgs_ShouldThrow()
        {
            // Arrange.
            await this.Initialize(CancellationToken.None);
            var builder = new WatchArgsBuilder(this.callbackRepository);

            var callback = await this.callbackRepository.AddAsync(
                builder.url,
                builder.callbackUrl,
                CancellationToken.None);

            // Assert.
            _ = Assert.ThrowsAsync<ArgumentNullException>(
                "transaction",
                () => this.subject.AddTransactionAsync(
                    null, builder.confirmation, builder.timeout,
                    callback, builder.successData, builder.timeoutData, CancellationToken.None)
            );

            _ = Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => this.subject.AddTransactionAsync(
                    builder.transaction, 0, builder.timeout,
                    callback, builder.successData, builder.timeoutData, CancellationToken.None)
            );

            _ = Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => this.subject.AddTransactionAsync(
                    builder.transaction, builder.confirmation, Ztm.Threading.Timer.MinDuration - TimeSpan.FromSeconds(1),
                    callback, builder.successData, builder.timeoutData, CancellationToken.None)
            );

            _ = Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => this.subject.AddTransactionAsync(
                    builder.transaction, builder.confirmation, Ztm.Threading.Timer.MaxDuration + TimeSpan.FromSeconds(1),
                    callback, builder.successData, builder.timeoutData, CancellationToken.None)
            );

            _ = Assert.ThrowsAsync<ArgumentNullException>(
                "callback",
                () => this.subject.AddTransactionAsync(
                    builder.transaction, builder.confirmation, builder.timeout,
                    null, builder.successData, builder.timeoutData, CancellationToken.None)
            );

            _ = Assert.ThrowsAsync<ArgumentNullException>(
                "successData",
                () => this.subject.AddTransactionAsync(
                    builder.transaction, builder.confirmation, builder.timeout,
                    callback, null, builder.timeoutData, CancellationToken.None)
            );

            _ = Assert.ThrowsAsync<ArgumentNullException>(
                "timeoutData",
                () => this.subject.AddTransactionAsync(
                    builder.transaction, builder.confirmation, builder.timeout,
                    callback, builder.successData, null, CancellationToken.None)
            );
        }

        [Fact]
        public async Task AddTransactionAsync_AndPushNoEvent_ShouldTimeout()
        {
            // Arrange.
            await this.Initialize(CancellationToken.None);
            var builder = new WatchArgsBuilder(this.callbackRepository);
            builder.timeout = TimeSpan.FromMilliseconds(500);

            // Act.
            await builder.Call(this.subject.AddTransactionAsync);
            Thread.Sleep(TimeSpan.FromMilliseconds(1000));

            // Assert.
            _ = this.callbackExecuter
                .Received(1)
                .Execute
                (
                    Arg.Any<Guid>(),
                    Arg.Any<Uri>(),
                    Arg.Is<TransactionConfirmationCallbackResult>(r => r.Status == CallbackResult.StatusError)
                );
        }

        [Fact]
        public async Task AddTransactionAsync_AndPushBlockWhichContainTransaction_TimerShouldBeStopped()
        {
            // Arrange.
            await this.Initialize(CancellationToken.None);

            var (block, _) = GenerateBlock();
            block.AddTransaction(Transaction.Create(ZcoinNetworks.Instance.Regtest));

            var builder = new WatchArgsBuilder(this.callbackRepository);
            builder.timeout = TimeSpan.FromSeconds(1);
            builder.transaction = block.Transactions[0].GetHash();

            await builder.Call(this.subject.AddTransactionAsync);

            // Act.
            await this.blockListener.BlockAddedAsync(block, 1, CancellationToken.None);
            Thread.Sleep(TimeSpan.FromSeconds(1));

            // Assert.
            this.callbackExecuter.Received(0);
        }

        [Fact]
        public async Task AddTransactionAsync_AndPushUntilRequiredConfirmation_ShouldCallSuccess()
        {
            // Arrange.
            await this.Initialize(CancellationToken.None);

            var (block, _) = GenerateBlock();
            block.AddTransaction(Transaction.Create(ZcoinNetworks.Instance.Regtest));

            var builder = new WatchArgsBuilder(this.callbackRepository);
            builder.confirmation = 10;
            builder.timeout = TimeSpan.FromSeconds(1);
            builder.transaction = block.Transactions[0].GetHash();

            await builder.Call(this.subject.AddTransactionAsync);

            // Act.
            await this.blockListener.BlockAddedAsync(block, 1, CancellationToken.None);
            for (var i = 0; i < 9; i++)
            {
                int height;
                (block, height) = GenerateBlock();

                await this.blockListener.BlockAddedAsync(block, height, CancellationToken.None);
            }
            Thread.Sleep(TimeSpan.FromMilliseconds(1));

            // Assert.
            _ = this.callbackExecuter
                .Received(1)
                .Execute
                (
                    Arg.Any<Guid>(),
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
            await this.Initialize(CancellationToken.None);

            var (block, _) = GenerateBlock();
            block.AddTransaction(Transaction.Create(ZcoinNetworks.Instance.Regtest));

            var builder = new WatchArgsBuilder(this.callbackRepository);
            builder.timeout = TimeSpan.FromMilliseconds(500);
            builder.transaction = block.Transactions[0].GetHash();

            await builder.Call(this.subject.AddTransactionAsync);

            // Act.
            await this.blockListener.BlockAddedAsync(block, 1, CancellationToken.None);
            await this.blockListener.BlockRemovingAsync(block, 1, CancellationToken.None);
            Thread.Sleep(TimeSpan.FromMilliseconds(1000));

            // Assert.
            _ = this.callbackExecuter
                .Received(1)
                .Execute
                (
                    Arg.Any<Guid>(),
                    Arg.Any<Uri>(),
                    Arg.Is<TransactionConfirmationCallbackResult>
                    (
                        r => r.Status == CallbackResult.StatusError
                    )
                );
        }

        [Fact]
        public async void AddTransactionAsync_WithValidArgument_ShouldSuccess()
        {
            // Arrange.
            var builder = new WatchArgsBuilder(this.callbackRepository);
            builder.timeout = TimeSpan.FromMilliseconds(500);

            // Act.
            await builder.Call(this.subject.AddTransactionAsync);
            Thread.Sleep(TimeSpan.FromMilliseconds(1000));

            // Assert.
            _ = this.callbackRepository.Received(1).AddAsync
            (
                Arg.Any<IPAddress>(),
                Arg.Any<Uri>(),
                Arg.Any<CancellationToken>()
            );

            _ = this.watchRepository.Received(1).AddAsync
            (
                Arg.Any<uint256>(),
                Arg.Any<int>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<TransactionConfirmationCallbackResult>(),
                Arg.Any<TransactionConfirmationCallbackResult>(),
                Arg.Any<Callback>(),
                Arg.Any<CancellationToken>()
            );

            _ = this.callbackExecuter.Received(1).Execute(
                Arg.Any<Guid>(),
                this.defaultUrl,
                Arg.Is<TransactionConfirmationCallbackResult>
                (
                    result => result == builder.timeoutData
                )
            );

            _ = this.callbackRepository.Received(1).AddHistoryAsync
            (
                Arg.Any<Guid>(),
                Arg.Any<TransactionConfirmationCallbackResult>(),
                Arg.Any<CancellationToken>()
            );

            _ = this.callbackRepository.Received(1).SetCompletedAsyc
            (
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>()
            );
        }

        [Fact]
        public async void AddTransactionAsync_AndFailToExecuteCallback_CompletedFlagShouldNotBeSet()
        {
            // Arrange.
            var builder = new WatchArgsBuilder(this.callbackRepository);
            builder.timeout = TimeSpan.FromMilliseconds(500);

            this.callbackExecuter
                .When(
                    w => w.Execute(
                        Arg.Any<Guid>(),
                        Arg.Any<Uri>(),
                        Arg.Any<CallbackResult>())
                )
                .Do(
                    w => {
                        throw new HttpRequestException();
                    }
                );

            // Act.
            await builder.Call(this.subject.AddTransactionAsync);
            Thread.Sleep(TimeSpan.FromMilliseconds(1000));

            // Assert.
            _ = this.watchRepository.Received(1).CompleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());

            _ = this.callbackExecuter.Received(1)
                .Execute
                (
                    Arg.Any<Guid>(),
                    Arg.Any<Uri>(),
                    Arg.Any<CallbackResult>()
                );

            _ = this.callbackRepository.Received(0).SetCompletedAsyc
            (
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>()
            );
        }

        [Fact]
        public async void AddTransactionAsync_AndSuccessToExecuteCallback_CompletedFlagShouldBeSet()
        {
            // Arrange.
            var builder = new WatchArgsBuilder(this.callbackRepository);
            builder.timeout = TimeSpan.FromMilliseconds(500);

            // Act.
            await builder.Call(this.subject.AddTransactionAsync);
            Thread.Sleep(TimeSpan.FromMilliseconds(1000));

            // Assert.
            _ = this.watchRepository.Received(1).CompleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());

            _ = this.callbackExecuter.Received(1)
                .Execute
                (
                    Arg.Any<Guid>(),
                    Arg.Any<Uri>(),
                    Arg.Any<CallbackResult>()
                );

            _ = this.callbackRepository.Received(1)
                .SetCompletedAsyc
                (
                    Arg.Any<Guid>(),
                    Arg.Any<CancellationToken>()
                );
        }

        [Fact]
        public async void AddTransactionAsync_AndWaitSomeWatchesToTimeout_ShouldCallExecute()
        {
            // Arrange.
            var builder = new WatchArgsBuilder(this.callbackRepository);

            // Act.
            builder.timeout = TimeSpan.FromSeconds(1);
            await builder.Call(this.subject.AddTransactionAsync);

            builder.timeout = TimeSpan.FromSeconds(2);
            await builder.Call(this.subject.AddTransactionAsync);

            builder.timeout = TimeSpan.FromDays(1);
            await builder.Call(this.subject.AddTransactionAsync);

            Thread.Sleep(TimeSpan.FromSeconds(3));

            // Assert.
            _ = this.callbackExecuter.Received(2).Execute(
                Arg.Any<Guid>(),
                this.defaultUrl,
                Arg.Is<TransactionConfirmationCallbackResult>
                (
                    result => result == builder.timeoutData
                )
            );
        }

        [Fact]
        public async Task CreateContextsAsync_WithValidArgs_ShouldSuccess()
        {
            // Arrange.
            var tx = Transaction.Parse(TransactionData.Transaction1, ZcoinNetworks.Instance.Mainnet);
            var untrackedTx = Transaction.Parse(TransactionData.Transaction2, ZcoinNetworks.Instance.Mainnet);

            var builder = new WatchArgsBuilder(this.callbackRepository);

            builder.timeout = TimeSpan.FromSeconds(1);
            builder.transaction = tx.GetHash();
            await builder.Call(this.subject.AddTransactionAsync);

            builder.timeout = TimeSpan.FromSeconds(2);
            await builder.Call(this.subject.AddTransactionAsync);

            // Act.
            var contexts = await this.handler.CreateContextsAsync(tx, CancellationToken.None);
            var untrackedTxContexts = await this.handler.CreateContextsAsync(untrackedTx, CancellationToken.None);

            // Assert.
            Assert.Equal(2, contexts.Count());
            Assert.Empty(untrackedTxContexts);
        }

        [Fact]
        public async void AddWatchesAsync_WithValidArgs_ShouldNotThrow()
        {
            // Arrange.
            var builder = new WatchArgsBuilder(this.callbackRepository);
            var watch1 = await builder.Call(this.subject.AddTransactionAsync);
            var watch2 = await builder.Call(this.subject.AddTransactionAsync);

            var ids = new List<TransactionWatch<Guid>>()
            {
                new TransactionWatch<Guid>(watch1.Id, uint256.Zero, uint256.Zero),
                new TransactionWatch<Guid>(watch2.Id, uint256.Zero, uint256.Zero),
            };

            // Act.
            await this.handler.AddWatchesAsync(ids, CancellationToken.None);
        }

        [Fact]
        public async void AddWatchesAsync_WithNullWatches_ShouldThrow()
        {
            await Assert.ThrowsAsync<ArgumentNullException>
            (
                "watches",
                () => this.handler.AddWatchesAsync(null, CancellationToken.None)
            );
        }

        [Fact]
        public async void GetCurrentWatchesAsync_WithNonEmpty_ShouldReceivedWatches()
        {
            // Arrange.
            var builder = new WatchArgsBuilder(this.callbackRepository);
            var watch1 = await builder.Call(this.subject.AddTransactionAsync);
            var watch2 = await builder.Call(this.subject.AddTransactionAsync);

            var watches = new List<TransactionWatch<Guid>>()
            {
                new TransactionWatch<Guid>(watch1.Id, uint256.Zero, uint256.Zero),
                new TransactionWatch<Guid>(watch2.Id, uint256.Zero, uint256.Zero),
            };

            await this.handler.AddWatchesAsync(watches, CancellationToken.None);

            // Act.
            var received = await this.handler.GetCurrentWatchesAsync(CancellationToken.None);

            // Assert.
            Assert.Equal(2, received.Count());
            Assert.Contains(received, w => w.Context == watch1.Id);
            Assert.Contains(received, w => w.Context == watch2.Id);
        }

        [Fact]
        public async void GetCurrentWatchesAsync_Empty_ShouldReceivedEmpty()
        {
            Assert.Empty(await this.handler.GetCurrentWatchesAsync(CancellationToken.None));
        }

        [Fact]
        // Timer must be stopped but watch object still is in the handler
        public async void ConfirmationUpdateAsync_WithValidWatch_ShouldSuccess()
        {
            // Arrange.
            var builder = new WatchArgsBuilder(this.callbackRepository);

            builder.timeout = TimeSpan.FromSeconds(1);
            var watch = await builder.Call(this.subject.AddTransactionAsync);

            var watches = new List<TransactionWatch<Guid>>()
            {
                new TransactionWatch<Guid>(watch.Id, uint256.Zero, builder.transaction),
            };

            await this.handler.AddWatchesAsync(watches, CancellationToken.None);

            // Act.
            var result = await this.handler.ConfirmationUpdateAsync(watches[0], 1, ConfirmationType.Confirmed, CancellationToken.None);
            Thread.Sleep(TimeSpan.FromSeconds(2));

            // Assert.
            Assert.False(result);

            _ = this.callbackExecuter.Received(0).Execute(
                Arg.Any<Guid>(),
                Arg.Any<Uri>(),
                Arg.Any<TransactionConfirmationCallbackResult>()
            );

            var received = await this.handler.GetCurrentWatchesAsync(CancellationToken.None);
            Assert.Single(received);
            Assert.Equal(watches[0].Id, received.First().Id);
        }

        [Fact]
        public async void ConfirmationUpdateAsync_AndReachRequiredConfirmations_ShouldCallSuccess()
        {
            // Arrange.
            var builder = new WatchArgsBuilder(this.callbackRepository);

            builder.timeout = TimeSpan.FromSeconds(2);
            builder.confirmation = 10;
            var watch = await builder.Call(this.subject.AddTransactionAsync);

            var watches = new List<TransactionWatch<Guid>>()
            {
                new TransactionWatch<Guid>(watch.Id, uint256.Zero, builder.transaction),
            };

            await this.handler.AddWatchesAsync(watches, CancellationToken.None);

            // Act.
            for (var confirmation = 1; confirmation <= builder.confirmation; confirmation++)
            {
                await this.handler.ConfirmationUpdateAsync(watches[0], confirmation, ConfirmationType.Confirmed, CancellationToken.None);
            }
            Thread.Sleep(TimeSpan.FromSeconds(2));

            // Assert.
            _ = this.callbackExecuter.Received(1).Execute(
                Arg.Any<Guid>(),
                Arg.Any<Uri>(),
                Arg.Is<TransactionConfirmationCallbackResult>
                (
                    result => result.Status == CallbackResult.StatusSuccess
                )
            );
        }

        [Fact]
        // Confirm before timeout and meet required after timeout
        public async void ConfirmationUpdateAsync_AndConfirmAfterTimeout_ShouldSuccess()
        {
            // Arrange.
            var builder = new WatchArgsBuilder(this.callbackRepository);

            builder.timeout = TimeSpan.FromSeconds(1);
            builder.confirmation = 10;
            var watch = await builder.Call(this.subject.AddTransactionAsync);

            var watches = new List<TransactionWatch<Guid>>()
            {
                new TransactionWatch<Guid>(watch.Id, uint256.Zero, builder.transaction),
            };

            await this.handler.AddWatchesAsync(watches, CancellationToken.None);

            // Act.
            for (var confirmation = 1; confirmation < builder.confirmation; confirmation++)
            {
                await this.handler.ConfirmationUpdateAsync(watches[0], confirmation, ConfirmationType.Confirmed, CancellationToken.None);
            }
            Thread.Sleep(TimeSpan.FromSeconds(2));
            await this.handler.ConfirmationUpdateAsync(watches[0], builder.confirmation, ConfirmationType.Confirmed, CancellationToken.None);

            // Assert.
            _ = this.callbackExecuter.Received(1).Execute(
                Arg.Any<Guid>(),
                Arg.Any<Uri>(),
                Arg.Is<TransactionConfirmationCallbackResult>
                (
                    result => result.Status == CallbackResult.StatusSuccess
                )
            );
        }

        [Fact]
        // Resume and Timeout
        public async void ConfirmationUpdateAsync_WithUnconfirm_TimerShouldbeResume()
        {
            // Arrange.
            var builder = new WatchArgsBuilder(this.callbackRepository);

            builder.timeout = TimeSpan.FromSeconds(1);
            builder.confirmation = 10;
            var watch = await builder.Call(this.subject.AddTransactionAsync);

            var watches = new List<TransactionWatch<Guid>>()
            {
                new TransactionWatch<Guid>(watch.Id, uint256.Zero, builder.transaction),
            };

            await this.handler.AddWatchesAsync(watches, CancellationToken.None);

            // Act.
            await this.handler.ConfirmationUpdateAsync(watches[0], 1, ConfirmationType.Confirmed, CancellationToken.None);
            await this.handler.ConfirmationUpdateAsync(watches[0], 2, ConfirmationType.Confirmed, CancellationToken.None);
            await this.handler.ConfirmationUpdateAsync(watches[0], 2, ConfirmationType.Unconfirming, CancellationToken.None);
            await this.handler.ConfirmationUpdateAsync(watches[0], 1, ConfirmationType.Unconfirming, CancellationToken.None);

            Thread.Sleep(TimeSpan.FromSeconds(2));

            // Assert.
            _ = this.callbackExecuter.Received(1).Execute(
                Arg.Any<Guid>(),
                Arg.Any<Uri>(),
                Arg.Is<TransactionConfirmationCallbackResult>
                (
                    result => result.Status == CallbackResult.StatusError
                )
            );
        }

        [Fact]
        public async void ConfirmationUpdateAsync_WithRemainingTimeout_ShouldNotExecuteTimeoutImmediately()
        {
            // Arrange.
            var builder = new WatchArgsBuilder(this.callbackRepository);

            builder.timeout = TimeSpan.FromMilliseconds(500);
            builder.confirmation = 10;
            var watch = await builder.Call(this.subject.AddTransactionAsync);

            var watches = new List<TransactionWatch<Guid>>()
            {
                new TransactionWatch<Guid>(watch.Id, uint256.Zero, builder.transaction),
            };

            await this.handler.AddWatchesAsync(watches, CancellationToken.None);

            // Act & Assert.
            await this.handler.ConfirmationUpdateAsync(watches[0], 1, ConfirmationType.Confirmed, CancellationToken.None);
            Thread.Sleep(TimeSpan.FromSeconds(2));

            var updated = await this.watchRepository.GetAsync(watch.Id, CancellationToken.None);
            Assert.True(updated.RemainingWaitingTime < TimeSpan.FromSeconds(1));

            await this.handler.ConfirmationUpdateAsync(watches[0], 1, ConfirmationType.Unconfirming, CancellationToken.None);

            _ = this.callbackExecuter.Received(0);

            Thread.Sleep(TimeSpan.FromMilliseconds(1000));

            _ = this.callbackExecuter.Received(1).Execute(
                Arg.Any<Guid>(),
                Arg.Any<Uri>(),
                Arg.Is<TransactionConfirmationCallbackResult>
                (
                    result => result.Status == CallbackResult.StatusError
                )
            );
        }

        [Fact]
        public async void RemoveWatchAsync_WithExistKey_ShouldSuccess()
        {
            // Arrange.
            var builder = new WatchArgsBuilder(this.callbackRepository);

            builder.timeout = TimeSpan.FromSeconds(1);
            builder.confirmation = 10;
            var watch = await builder.Call(this.subject.AddTransactionAsync);

            var watches = new List<TransactionWatch<Guid>>()
            {
                new TransactionWatch<Guid>(watch.Id, uint256.Zero, builder.transaction),
            };

            await this.handler.AddWatchesAsync(watches, CancellationToken.None);

            // Act.
            await this.handler.RemoveWatchAsync(watches[0], WatchRemoveReason.Completed, CancellationToken.None);

            // Assert.
            Assert.Empty(await this.handler.GetCurrentWatchesAsync(CancellationToken.None));
        }

        [Fact]
        public async void Initialize_WithNonEmptyRepository_ShouldInitializeWatches()
        {
            // Arrange.
            var tx = Transaction.Parse(TransactionData.Transaction1, ZcoinNetworks.Instance.Mainnet);

            var builder = new WatchArgsBuilder(this.callbackRepository);
            builder.timeout = TimeSpan.FromSeconds(1);
            builder.transaction = tx.GetHash();

            var callback = await this.callbackRepository.AddAsync
            (
                builder.url, builder.callbackUrl, CancellationToken.None
            );

            _ = await this.watchRepository.AddAsync
            (
                builder.transaction, builder.confirmation, builder.timeout, builder.successData, builder.timeoutData, callback, CancellationToken.None
            );

            // Completed watch
            var completedCallback = new Callback
            (
                Guid.NewGuid(),
                IPAddress.Loopback,
                DateTime.UtcNow,
                true,
                this.defaultUrl
            );

            var watch = await this.watchRepository.AddAsync
            (
                builder.transaction, builder.confirmation, builder.timeout, builder.successData, builder.timeoutData, completedCallback, CancellationToken.None
            );

            await this.watchRepository.CompleteAsync(watch.Id, CancellationToken.None);

            // Act.
            ITransactionConfirmationWatcherHandler<Guid> localHandler;
            TransactionConfirmationWatcher localWatcher;

            localHandler = localWatcher = new TransactionConfirmationWatcher
            (
                this.callbackRepository,
                this.watchRepository,
                this.blockStorage,
                this.callbackExecuter
            );

            await localWatcher.StartAsync(CancellationToken.None);
            var retrievedCount = (await localHandler.CreateContextsAsync(tx, CancellationToken.None)).Count();
            Thread.Sleep(TimeSpan.FromSeconds(2));

            // Assert.
            Assert.Equal(1, retrievedCount);

            _ = this.callbackExecuter.Received(1).Execute(
                Arg.Any<Guid>(),
                Arg.Any<Uri>(),
                Arg.Is<TransactionConfirmationCallbackResult>
                (
                    result => result.Status == CallbackResult.StatusError
                )
            );
        }

        readonly Dictionary<uint256, Tuple<Block, int>> mockedBlocks = new Dictionary<uint256, Tuple<Block, int>>();
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

        Dictionary<Guid, TransactionConfirmationWatch<TransactionConfirmationCallbackResult>> mockedWatchs;

        void MockWatchRepository()
        {
            mockedWatchs = new Dictionary<Guid, TransactionConfirmationWatch<TransactionConfirmationCallbackResult>>();

            this.watchRepository
                .When
                (
                    w => w.CompleteAsync(
                        Arg.Any<Guid>(),
                        Arg.Any<CancellationToken>()
                    )
                )
                .Do
                (
                    w =>
                    {
                        var id = w.ArgAt<Guid>(0);

                        var old = mockedWatchs[id];

                        mockedWatchs[id] = new TransactionConfirmationWatch<TransactionConfirmationCallbackResult>(
                            old.Id, old.Transaction, true, old.Confirmation, old.WaitingTime, old.RemainingWaitingTime,
                            old.Success, old.Timeout, old.Callback);
                    }
                );

            this.watchRepository
                .AddAsync
                (
                    Arg.Any<uint256>(),
                    Arg.Any<int>(),
                    Arg.Any<TimeSpan>(),
                    Arg.Any<TransactionConfirmationCallbackResult>(),
                    Arg.Any<TransactionConfirmationCallbackResult>(),
                    Arg.Any<Callback>(),
                    Arg.Any<CancellationToken>()
                )
                .Returns
                (
                    info =>
                    {
                        var watch = new TransactionConfirmationWatch<TransactionConfirmationCallbackResult>
                        (
                            Guid.NewGuid(),
                            info.ArgAt<uint256>(0),
                            false,
                            info.ArgAt<int>(1),
                            info.ArgAt<TimeSpan>(2),
                            info.ArgAt<TimeSpan>(2),
                            info.ArgAt<TransactionConfirmationCallbackResult>(3),
                            info.ArgAt<TransactionConfirmationCallbackResult>(4),
                            info.ArgAt<Callback>(5)
                        );

                        mockedWatchs[watch.Id] = watch;

                        return Task.FromResult(watch);
                    }
                );

                this.watchRepository
                    .GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                    .Returns(info => mockedWatchs[info.ArgAt<Guid>(0)]);

                this.watchRepository
                    .ListAsync(Arg.Any<CancellationToken>())
                    .Returns(info => mockedWatchs.Select(w => w.Value));

                this.watchRepository
                    .When(w => w.SetRemainingWaitingTimeAsync(Arg.Any<Guid>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()))
                    .Do(w => {
                        var id = w.ArgAt<Guid>(0);
                        var remaining = w.ArgAt<TimeSpan>(1);

                        var old = mockedWatchs[id];

                        mockedWatchs[id] = new TransactionConfirmationWatch<TransactionConfirmationCallbackResult>(
                            old.Id, old.Transaction, old.Completed, old.Confirmation, old.WaitingTime, remaining,
                            old.Success, old.Timeout, old.Callback);
                    });
        }

        void MockCallbackRepository()
        {
            this.callbackRepository
                .AddAsync
                (
                    Arg.Any<IPAddress>(),
                    Arg.Any<Uri>(),
                    Arg.Any<CancellationToken>()
                )
                .Returns
                (
                    info =>
                        Task.FromResult(
                            new Callback
                            (
                                Guid.NewGuid(),
                                info.ArgAt<IPAddress>(0),
                                DateTime.UtcNow,
                                false,
                                info.ArgAt<Uri>(1)
                            )
                        )
                );
        }

        public class WatchArgsBuilder
        {
            public uint256 transaction;
            public int confirmation;
            public TimeSpan timeout;
            public IPAddress url;
            public Uri callbackUrl;
            public TransactionConfirmationCallbackResult successData;
            public TransactionConfirmationCallbackResult timeoutData;
            public CancellationToken cancellationToken;

            readonly ICallbackRepository callbackRepository;

            public WatchArgsBuilder(ICallbackRepository callbackRepository)
            {
                this.callbackRepository = callbackRepository;

                this.transaction = uint256.Parse("7396ddaa275ed5492564277efc0844b4aeaa098020bc8d4b4dbc489134e49afd");
                this.confirmation = 10;
                this.timeout = TimeSpan.FromSeconds(1);
                this.url = IPAddress.Loopback;
                this.callbackUrl = new Uri("http://zcoin.io");
                this.successData = new TransactionConfirmationCallbackResult(CallbackResult.StatusSuccess, "success");
                this.timeoutData = new TransactionConfirmationCallbackResult(CallbackResult.StatusError, "timeout");
                this.cancellationToken = CancellationToken.None;
            }

            public async Task<T> Call<T>(Func<uint256, int, TimeSpan, Callback,
                TransactionConfirmationCallbackResult,
                TransactionConfirmationCallbackResult,
                CancellationToken, Task<T>> func)
            {
                var callback = await this.callbackRepository.AddAsync(
                    this.url,
                    this.callbackUrl,
                    CancellationToken.None);

                return await func(transaction, confirmation, timeout, callback,
                    successData, timeoutData, cancellationToken);
            }
        }
    }
}