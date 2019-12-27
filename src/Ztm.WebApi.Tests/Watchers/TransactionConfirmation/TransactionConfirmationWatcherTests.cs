using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NBitcoin;
using NSubstitute;
using Xunit;
using Ztm.Testing;
using Ztm.WebApi.Callbacks;
using Ztm.WebApi.Watchers.TransactionConfirmation;
using Ztm.Zcoin.NBitcoin;
using Ztm.Zcoin.Synchronization;
using Ztm.Zcoin.Watching;
using Callback = Ztm.WebApi.Callbacks.Callback;

namespace Ztm.WebApi.Tests.Watchers.TransactionConfirmation
{
    public sealed class TransactionConfirmationWatcherTests : IDisposable
    {
        readonly IBlockListener blockListener;
        readonly TransactionConfirmationWatcher subject;
        readonly ITransactionConfirmationWatcherHandler<Rule> handler;

        readonly ICallbackRepository callbackRepository;
        readonly FakeRuleRepository ruleRepository;
        readonly IBlocksStorage blockStorage;
        readonly IWatchRepository watchRepository;
        readonly ICallbackExecuter callbackExecuter;
        readonly ILogger<TransactionConfirmationWatcher> logger;

        readonly Uri defaultUrl;

        volatile bool initialized = false;

        public TransactionConfirmationWatcherTests()
        {
            this.callbackRepository = Substitute.For<ICallbackRepository>();
            this.ruleRepository = Substitute.ForPartsOf<FakeRuleRepository>();

            this.blockStorage = Substitute.For<IBlocksStorage>();
            this.blockStorage
                .GetAsync(Arg.Any<uint256>(), Arg.Any<CancellationToken>())
                .Returns(info => mockedBlocks[info.ArgAt<uint256>(0)].ToValueTuple());

            this.callbackExecuter = Substitute.For<ICallbackExecuter>();
            this.logger = Substitute.For<ILogger<TransactionConfirmationWatcher>>();
            this.watchRepository = new FakeWatchRepository();

            this.handler = this.subject = new TransactionConfirmationWatcher
            (
                this.callbackRepository,
                this.ruleRepository,
                this.blockStorage,
                this.callbackExecuter,
                this.watchRepository,
                this.logger
            );
            this.blockListener = this.subject;
            this.defaultUrl = new Uri("http://zcoin.io");

            MockCallbackRepository();
        }

        public void Dispose()
        {
            if (initialized)
            {
                this.subject.StopAsync(CancellationToken.None).Wait();
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
                this.ruleRepository,
                this.blockStorage,
                this.callbackExecuter,
                this.watchRepository,
                this.logger
            );
        }

        [Fact]
        public void Construct_WithNullArg_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>
            (
                "callbackRepository",
                () => new TransactionConfirmationWatcher(null, this.ruleRepository, this.blockStorage, this.callbackExecuter, this.watchRepository, this.logger)
            );

            Assert.Throws<ArgumentNullException>
            (
                "ruleRepository",
                () => new TransactionConfirmationWatcher(this.callbackRepository, null, this.blockStorage, this.callbackExecuter, this.watchRepository, this.logger)
            );

            Assert.Throws<ArgumentNullException>
            (
                "blocks",
                () => new TransactionConfirmationWatcher(this.callbackRepository, this.ruleRepository, null, this.callbackExecuter, this.watchRepository, this.logger)
            );

            Assert.Throws<ArgumentNullException>
            (
                "callbackExecuter",
                () => new TransactionConfirmationWatcher(this.callbackRepository, this.ruleRepository, this.blockStorage, null, this.watchRepository, this.logger)
            );

            Assert.Throws<ArgumentNullException>
            (
                "watchRepository",
                () => new TransactionConfirmationWatcher(this.callbackRepository, this.ruleRepository, this.blockStorage, this.callbackExecuter, null, this.logger)
            );

            Assert.Throws<ArgumentNullException>
            (
                "logger",
                () => new TransactionConfirmationWatcher(this.callbackRepository, this.ruleRepository, this.blockStorage, this.callbackExecuter, this.watchRepository, null)
            );
        }

        [Fact]
        public async Task AddTransactionAsync_WithNullArgs_ShouldThrow()
        {
            // Arrange.
            await this.Initialize(CancellationToken.None);
            var builder = new WatchArgsBuilder(this.callbackRepository);

            var callback = await this.callbackRepository.AddAsync(
                builder.Ip,
                builder.CallbackUrl,
                CancellationToken.None);

            // Assert.
            await Assert.ThrowsAsync<ArgumentNullException>(
                "transaction",
                () => this.subject.AddTransactionAsync(
                    null, builder.Confirmations, builder.Timeout,
                    callback, builder.SuccessData, builder.TimeoutData, CancellationToken.None)
            );

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                "confirmation",
                () => this.subject.AddTransactionAsync(
                    builder.Transaction, 0, builder.Timeout,
                    callback, builder.SuccessData, builder.TimeoutData, CancellationToken.None)
            );

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                "unconfirmedWaitingTime",
                () => this.subject.AddTransactionAsync(
                    builder.Transaction, builder.Confirmations, Ztm.Threading.TimerSchedulers.ThreadPoolScheduler.MaxDuration + TimeSpan.FromSeconds(1),
                    callback, builder.SuccessData, builder.TimeoutData, CancellationToken.None)
            );

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                "unconfirmedWaitingTime",
                () => this.subject.AddTransactionAsync(
                    builder.Transaction, builder.Confirmations, Ztm.Threading.TimerSchedulers.ThreadPoolScheduler.MinDuration - TimeSpan.FromSeconds(1),
                    callback, builder.SuccessData, builder.TimeoutData, CancellationToken.None)
            );

            await Assert.ThrowsAsync<ArgumentNullException>(
                "callback",
                () => this.subject.AddTransactionAsync(
                    builder.Transaction, builder.Confirmations, builder.Timeout,
                    null, builder.SuccessData, builder.TimeoutData, CancellationToken.None)
            );

            await Assert.ThrowsAsync<ArgumentNullException>(
                "successResponse",
                () => this.subject.AddTransactionAsync(
                    builder.Transaction, builder.Confirmations, builder.Timeout,
                    callback, null, builder.TimeoutData, CancellationToken.None)
            );

            await Assert.ThrowsAsync<ArgumentNullException>(
                "timeoutResponse",
                () => this.subject.AddTransactionAsync(
                    builder.Transaction, builder.Confirmations, builder.Timeout,
                    callback, builder.SuccessData, null, CancellationToken.None)
            );
        }

        [Fact]
        public async Task AddTransactionAsync_WithNullArguments_ShouldThrow()
        {
            var builder = new WatchArgsBuilder(this.callbackRepository);
            var callback = await this.callbackRepository.AddAsync(
                builder.Ip,
                builder.CallbackUrl,
                CancellationToken.None);

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => this.subject.AddTransactionAsync(
                    uint256.One,
                    10,
                    TimeSpan.MinValue,
                    callback,
                    new CallbackResult("", ""),
                    new CallbackResult("", ""),
                    CancellationToken.None)
            );
        }

        [Fact]
        public async Task AddTransactionAsync_AndPushNoEvent_ShouldTimeout()
        {
            using (var elapsed = new ManualResetEventSlim())
            {
                // Arrange.
                await this.Initialize(CancellationToken.None);
                var builder = new WatchArgsBuilder(this.callbackRepository);
                builder.Timeout = TimeSpan.FromMilliseconds(200);
                this.callbackExecuter.When(e => e.ExecuteAsync(Arg.Any<Guid>(),Arg.Any<Uri>(), Arg.Any<CallbackResult>(), Arg.Any<CancellationToken>()))
                                     .Do(c => elapsed.Set());

                // Act.
                await builder.Call(this.subject.AddTransactionAsync);
                elapsed.Wait(1500);

                // Assert.
                _ = this.callbackExecuter
                    .Received(1)
                    .ExecuteAsync
                    (
                        Arg.Any<Guid>(),
                        Arg.Any<Uri>(),
                        Arg.Is<CallbackResult>(r => r.Status == CallbackResult.StatusError),
                        Arg.Any<CancellationToken>()
                    );
            }
        }

        [Fact]
        public async Task AddTransactionAsync_TimeoutAndFailToCallback_ShouldNotThrow()
        {
            using (var elapsed = new ManualResetEventSlim())
            {
                // Arrange.
                await this.Initialize(CancellationToken.None);
                var builder = new WatchArgsBuilder(this.callbackRepository);
                builder.Timeout = TimeSpan.FromMilliseconds(200);
                this.ruleRepository.When(r => r.UpdateStatusAsync(Arg.Any<Guid>(), Arg.Any<RuleStatus>(), Arg.Any<CancellationToken>()))
                                   .Throw(info => {
                                       elapsed.Set();
                                       return new Exception();
                                   });

                // Act.
                await builder.Call(this.subject.AddTransactionAsync);
                elapsed.Wait(1500);

                // Assert.
                _ = this.callbackExecuter
                    .Received(0)
                    .ExecuteAsync
                    (
                        Arg.Any<Guid>(),
                        Arg.Any<Uri>(),
                        Arg.Any<CallbackResult>(),
                        Arg.Any<CancellationToken>()
                    );
            }
        }

        [Fact]
        public async Task AddTransactionAsync_AndPushBlockWhichContainTransaction_TimerShouldBeStopped()
        {
            // Arrange.
            await this.Initialize(CancellationToken.None);

            var (block, _) = GenerateBlock();
            block.AddTransaction(Transaction.Create(ZcoinNetworks.Instance.Regtest));

            var builder = new WatchArgsBuilder(this.callbackRepository);
            builder.Timeout = TimeSpan.FromSeconds(1);
            builder.Transaction = block.Transactions[0].GetHash();

            await builder.Call(this.subject.AddTransactionAsync);

            // Act.
            await this.blockListener.BlockAddedAsync(block, 1, CancellationToken.None);
            Thread.Sleep(TimeSpan.FromSeconds(1));

            // Assert.
            _ = this.callbackExecuter.Received(0).ExecuteAsync(Arg.Any<Guid>(), Arg.Any<Uri>(), Arg.Any<CallbackResult>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task AddTransactionAsync_AndPushUntilRequiredConfirmation_ShouldCallSuccess()
        {
            // Arrange.
            await this.Initialize(CancellationToken.None);

            var (block, _) = GenerateBlock();
            block.AddTransaction(Transaction.Create(ZcoinNetworks.Instance.Regtest));

            var builder = new WatchArgsBuilder(this.callbackRepository);
            builder.Confirmations = 10;
            builder.Timeout = TimeSpan.FromMilliseconds(500);
            builder.Transaction = block.Transactions[0].GetHash();

            var rule = await builder.Call(this.subject.AddTransactionAsync);

            // Act.
            await this.blockListener.BlockAddedAsync(block, 1, CancellationToken.None);
            for (var i = 0; i < builder.Confirmations - 1; i++)
            {
                int height;
                (block, height) = GenerateBlock();

                await this.blockListener.BlockAddedAsync(block, height, CancellationToken.None);
            }

            // Assert.
            _ = this.callbackExecuter
                .Received(1)
                .ExecuteAsync
                (
                    rule.Callback.Id,
                    rule.Callback.Url,
                    Arg.Is<CallbackResult>
                    (
                        r => r.Status == CallbackResult.StatusSuccess
                    ),
                    Arg.Any<CancellationToken>()
                );
        }

        [Fact]
        public async Task AddTransactionAsync_AndBlocksAreRemoved_TimersShouldBeResume()
        {
            // Arrange.
            await this.Initialize(CancellationToken.None);

            var (block, _) = GenerateBlock();
            block.AddTransaction(Transaction.Create(ZcoinNetworks.Instance.Regtest));

            var builder = new WatchArgsBuilder(this.callbackRepository);
            builder.Timeout = TimeSpan.FromMilliseconds(500);
            builder.Transaction = block.Transactions[0].GetHash();

            var rule = await builder.Call(this.subject.AddTransactionAsync);

            // Act.
            await this.blockListener.BlockAddedAsync(block, 1, CancellationToken.None);
            await this.blockListener.BlockRemovingAsync(block, 1, CancellationToken.None);
            Thread.Sleep(TimeSpan.FromMilliseconds(1000));

            // Assert.
            _ = this.callbackExecuter
                .Received(1)
                .ExecuteAsync
                (
                    rule.Callback.Id,
                    rule.Callback.Url,
                    Arg.Is<CallbackResult>
                    (
                        r => r.Status == CallbackResult.StatusError
                    ),
                    Arg.Any<CancellationToken>()
                );
        }

        [Fact]
        public async Task AddTransactionAsync_WithValidArgument_ShouldSuccess()
        {
            // Arrange.
            var builder = new WatchArgsBuilder(this.callbackRepository);
            builder.Timeout = TimeSpan.FromMilliseconds(500);

            // Act.
            var rule = await builder.Call(this.subject.AddTransactionAsync);
            Thread.Sleep(TimeSpan.FromMilliseconds(1000));

            // Assert.
            _ = this.callbackRepository.Received(1).AddAsync
            (
                Arg.Is<IPAddress>(ip => ip == builder.Ip),
                Arg.Is<Uri>(url => url == builder.CallbackUrl),
                Arg.Any<CancellationToken>()
            );

            _ = this.ruleRepository.Received(1).AddAsync
            (
                Arg.Is<uint256>(tx => tx == builder.Transaction),
                Arg.Is<int>(confirmations => confirmations == builder.Confirmations),
                Arg.Is<TimeSpan>(t => t == builder.Timeout),
                Arg.Is<CallbackResult>(r => r == builder.SuccessData),
                Arg.Is<CallbackResult>(r => r == builder.TimeoutData),
                Arg.Is<Callback>(c => c == rule.Callback),
                Arg.Any<CancellationToken>()
            );

            _ = this.callbackExecuter.Received(1).ExecuteAsync
            (
                Arg.Is<Guid>(id => id == rule.Callback.Id),
                this.defaultUrl,
                Arg.Is<CallbackResult>
                (
                    result => result == builder.TimeoutData
                ),
                Arg.Any<CancellationToken>()
            );

            _ = this.callbackRepository.Received(1).AddHistoryAsync
            (
                Arg.Is<Guid>(id => id == rule.Callback.Id),
                Arg.Is<CallbackResult>(r => r == rule.TimeoutResponse),
                Arg.Any<CancellationToken>()
            );

            _ = this.callbackRepository.Received(1).SetCompletedAsyc
            (
                Arg.Is<Guid>(id => id == rule.Callback.Id),
                Arg.Any<CancellationToken>()
            );
        }

        [Fact]
        public async Task AddTransactionAsync_AndFailToExecuteCallback_CompletedFlagAndHistorySuccessFlagShouldNotBeSet()
        {
            // Arrange.
            var builder = new WatchArgsBuilder(this.callbackRepository);
            builder.Timeout = TimeSpan.FromMilliseconds(500);

            this.callbackExecuter
                .When(
                    w => w.ExecuteAsync(
                        Arg.Any<Guid>(),
                        Arg.Any<Uri>(),
                        Arg.Any<CallbackResult>(),
                        Arg.Any<CancellationToken>())
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
            _ = this.ruleRepository.Received(1)
                    .UpdateStatusAsync(Arg.Any<Guid>(), Arg.Any<RuleStatus>(), Arg.Any<CancellationToken>());

            _ = this.callbackExecuter.Received(1)
                    .ExecuteAsync
                    (
                        Arg.Any<Guid>(),
                        Arg.Any<Uri>(),
                        Arg.Any<CallbackResult>(),
                        Arg.Any<CancellationToken>()
                    );

            _ = this.callbackRepository.Received(0)
                    .SetCompletedAsyc
                    (
                        Arg.Any<Guid>(),
                        Arg.Any<CancellationToken>()
                    );
        }

        [Fact]
        public async Task AddTransactionAsync_AndSuccessToExecuteCallback_CompletedFlagAndHistorySuccessFlagShouldBeSet()
        {
            // Arrange.
            var builder = new WatchArgsBuilder(this.callbackRepository);
            builder.Timeout = TimeSpan.FromMilliseconds(500);

            // Act.
            var rule = await builder.Call(this.subject.AddTransactionAsync);
            Thread.Sleep(TimeSpan.FromMilliseconds(1000));

            // Assert.
            _ = this.ruleRepository.Received(1).UpdateStatusAsync(rule.Id, Arg.Any<RuleStatus>(), Arg.Any<CancellationToken>());

            _ = this.callbackExecuter.Received(1).ExecuteAsync
            (
                Arg.Any<Guid>(),
                Arg.Any<Uri>(),
                Arg.Any<CallbackResult>(),
                Arg.Any<CancellationToken>()
            );

            _ = this.callbackRepository.Received(1).SetCompletedAsyc
            (
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>()
            );
        }

        [Fact]
        public async Task AddTransactionAsync_AndWaitSomeWatchesToTimeout_ShouldCallExecute()
        {
            // Arrange.
            var builder = new WatchArgsBuilder(this.callbackRepository);

            // Act.
            builder.Timeout = TimeSpan.FromSeconds(1);
            await builder.Call(this.subject.AddTransactionAsync);

            builder.Timeout = TimeSpan.FromSeconds(2);
            await builder.Call(this.subject.AddTransactionAsync);

            builder.Timeout = TimeSpan.FromDays(1);
            await builder.Call(this.subject.AddTransactionAsync);

            Thread.Sleep(TimeSpan.FromSeconds(3));

            // Assert.
            _ = this.callbackExecuter.Received(2).ExecuteAsync
            (
                Arg.Any<Guid>(),
                this.defaultUrl,
                Arg.Is<CallbackResult>
                (
                    result => result == builder.TimeoutData
                ),
                Arg.Any<CancellationToken>()
            );
        }

        [Fact]
        public async Task AddTransactionAsync_WithOnChainTransactionAndFork_ShouldBeResumeAndTimeout()
        {
            // Arrange.
            var transaction = Transaction.Parse(TestTransaction.Raw1, ZcoinNetworks.Instance.Regtest);
            var builder = new WatchArgsBuilder(this.callbackRepository);
            builder.Transaction = transaction.GetHash();

            this.blockStorage
                .GetTransactionAsync(transaction.GetHash(), Arg.Any<CancellationToken>())
                .Returns(transaction);

            var (block, _) = GenerateBlock();

            builder.Timeout = TimeSpan.FromMilliseconds(500);
            await builder.Call(this.subject.AddTransactionAsync);

            // Act.
            await this.blockListener.BlockRemovingAsync(block, 1, CancellationToken.None);
            Thread.Sleep(TimeSpan.FromMilliseconds(1000));

            // Assert.
            _ = this.callbackExecuter.Received(1).ExecuteAsync
            (
                Arg.Any<Guid>(),
                this.defaultUrl,
                Arg.Is<CallbackResult>
                (
                    result => result == builder.TimeoutData
                ),
                Arg.Any<CancellationToken>()
            );
        }

        [Fact]
        public async Task CreateContextsAsync_WithValidArgs_ShouldSuccess()
        {
            // Arrange.
            var tx = Transaction.Parse(TestTransaction.Raw1, ZcoinNetworks.Instance.Mainnet);
            var untrackedTx = Transaction.Parse(TestTransaction.Raw2, ZcoinNetworks.Instance.Mainnet);

            var builder = new WatchArgsBuilder(this.callbackRepository);

            builder.Timeout = TimeSpan.FromSeconds(1);
            builder.Transaction = tx.GetHash();
            await builder.Call(this.subject.AddTransactionAsync);

            builder.Timeout = TimeSpan.FromSeconds(2);
            await builder.Call(this.subject.AddTransactionAsync);

            // Act.
            var contexts = await this.handler.CreateContextsAsync(tx, CancellationToken.None);
            var untrackedTxContexts = await this.handler.CreateContextsAsync(untrackedTx, CancellationToken.None);

            // Assert.
            Assert.Equal(2, contexts.Count());
            Assert.Empty(untrackedTxContexts);
        }

        [Fact]
        public async Task AddWatchesAsync_WithValidArgs_ShouldNotThrow()
        {
            // Arrange.
            var builder = new WatchArgsBuilder(this.callbackRepository);
            var watch1 = await builder.Call(this.subject.AddTransactionAsync);
            var watch2 = await builder.Call(this.subject.AddTransactionAsync);

            var watches = new List<TransactionWatch<Rule>>()
            {
                new TransactionWatch<Rule>(watch1, uint256.Zero, uint256.Zero),
                new TransactionWatch<Rule>(watch2, uint256.Zero, uint256.Zero),
            };

            // Act.
            await this.handler.AddWatchesAsync(watches, CancellationToken.None);
            _ = this.ruleRepository.Received(1).UpdateCurrentWatchAsync
            (
                Arg.Is<Guid>(id => id == watch1.Id),
                Arg.Is<Guid>(id => id == watches[0].Id),
                Arg.Any<CancellationToken>()
            );

            _ = this.ruleRepository.Received(1).UpdateCurrentWatchAsync
            (
                Arg.Is<Guid>(id => id == watch2.Id),
                Arg.Is<Guid>(id => id == watches[1].Id),
                Arg.Any<CancellationToken>()
            );
        }

        [Fact]
        public async Task AddWatchesAsync_WithNullWatches_ShouldThrow()
        {
            await Assert.ThrowsAsync<ArgumentNullException>
            (
                "watches",
                () => this.handler.AddWatchesAsync(null, CancellationToken.None)
            );
        }

        [Fact]
        public async Task GetCurrentWatchesAsync_WithNonEmpty_ShouldReceivedWatches()
        {
            // Arrange.
            var builder = new WatchArgsBuilder(this.callbackRepository);
            var watch1 = await builder.Call(this.subject.AddTransactionAsync);
            var watch2 = await builder.Call(this.subject.AddTransactionAsync);

            var watches = new List<TransactionWatch<Rule>>()
            {
                new TransactionWatch<Rule>(watch1, uint256.Zero, watch1.TransactionHash),
                new TransactionWatch<Rule>(watch2, uint256.Zero, watch1.TransactionHash),
            };

            await this.handler.AddWatchesAsync(watches, CancellationToken.None);

            // Act.
            var received = await this.handler.GetCurrentWatchesAsync(CancellationToken.None);

            // Assert.
            Assert.Equal(2, received.Count());
            Assert.Contains(received, w => w.Context.Id == watch1.Id);
            Assert.Contains(received, w => w.Context.Id == watch2.Id);
        }

        [Fact]
        public async Task GetCurrentWatchesAsync_Empty_ShouldReceivedEmpty()
        {
            Assert.Empty(await this.handler.GetCurrentWatchesAsync(CancellationToken.None));
        }

        [Fact]
        public async Task ConfirmationUpdateAsync_WithValidWatch_ShouldSuccess()
        {
            // Arrange.
            var builder = new WatchArgsBuilder(this.callbackRepository);

            builder.Timeout = TimeSpan.FromSeconds(1);
            var watch = await builder.Call(this.subject.AddTransactionAsync);

            var watches = new List<TransactionWatch<Rule>>()
            {
                new TransactionWatch<Rule>(watch, uint256.Zero, builder.Transaction),
            };

            await this.handler.AddWatchesAsync(watches, CancellationToken.None);

            // Act.
            var result = await this.handler.ConfirmationUpdateAsync(watches[0], 1, ConfirmationType.Confirmed, CancellationToken.None);
            Thread.Sleep(TimeSpan.FromSeconds(2));

            // Assert.
            Assert.False(result);

            _ = this.callbackExecuter.Received(0).ExecuteAsync
            (
                Arg.Any<Guid>(),
                Arg.Any<Uri>(),
                Arg.Any<CallbackResult>(),
                Arg.Any<CancellationToken>()
            );

            var received = await this.handler.GetCurrentWatchesAsync(CancellationToken.None);
            Assert.Single(received);
            Assert.Equal(watches[0].Id, received.First().Id);
        }

        [Fact]
        public async Task ConfirmationUpdateAsync_AndReachRequiredConfirmations_ShouldCallSuccess()
        {
            // Arrange.
            var builder = new WatchArgsBuilder(this.callbackRepository);

            builder.Timeout = TimeSpan.FromSeconds(2);
            builder.Confirmations = 10;
            var rule = await builder.Call(this.subject.AddTransactionAsync);

            var watches = new List<TransactionWatch<Rule>>()
            {
                new TransactionWatch<Rule>(rule, uint256.Zero, builder.Transaction),
            };

            await this.handler.AddWatchesAsync(watches, CancellationToken.None);

            // Act.
            for (var confirmation = 1; confirmation <= builder.Confirmations; confirmation++)
            {
                await this.handler.ConfirmationUpdateAsync(watches[0], confirmation, ConfirmationType.Confirmed, CancellationToken.None);
            }
            Thread.Sleep(TimeSpan.FromSeconds(2));

            // Assert.
            _ = this.callbackExecuter.Received(1).ExecuteAsync
            (
                rule.Callback.Id,
                rule.Callback.Url,
                Arg.Is<CallbackResult>
                (
                    result => result.Status == CallbackResult.StatusSuccess
                ),
                Arg.Any<CancellationToken>()
            );
        }

        [Fact]
        // Confirm before timeout and meet required after timeout
        public async Task ConfirmationUpdateAsync_AndConfirmAfterTimeout_ShouldSuccess()
        {
            // Arrange.
            var builder = new WatchArgsBuilder(this.callbackRepository);

            builder.Timeout = TimeSpan.FromSeconds(1);
            builder.Confirmations = 10;
            var watch = await builder.Call(this.subject.AddTransactionAsync);

            var watches = new List<TransactionWatch<Rule>>()
            {
                new TransactionWatch<Rule>(watch, uint256.Zero, builder.Transaction),
            };

            await this.handler.AddWatchesAsync(watches, CancellationToken.None);

            // Act.
            for (var confirmation = 1; confirmation < builder.Confirmations; confirmation++)
            {
                await this.handler.ConfirmationUpdateAsync(watches[0], confirmation, ConfirmationType.Confirmed, CancellationToken.None);
            }
            Thread.Sleep(TimeSpan.FromSeconds(2));
            await this.handler.ConfirmationUpdateAsync(watches[0], builder.Confirmations, ConfirmationType.Confirmed, CancellationToken.None);

            // Assert.
            _ = this.callbackExecuter.Received(1).ExecuteAsync
            (
                Arg.Any<Guid>(),
                Arg.Any<Uri>(),
                Arg.Is<CallbackResult>
                (
                    result => result.Status == CallbackResult.StatusSuccess
                ),
                Arg.Any<CancellationToken>()
            );
        }

        [Fact]
        public async Task RemoveUncompletedWatchesAsync_TimerShouldbeResume()
        {
            // Arrange.
            var builder = new WatchArgsBuilder(this.callbackRepository);
            builder.Timeout = TimeSpan.FromMilliseconds(500);
            builder.Confirmations = 10;

            var rule = await builder.Call(this.subject.AddTransactionAsync);

            var watches = new List<TransactionWatch<Rule>>();
            var watch = new TransactionWatch<Rule>(rule, uint256.Zero, builder.Transaction);
            watches.Add(watch);

            await this.handler.AddWatchesAsync(watches, CancellationToken.None);

            // Act.
            await this.handler.RemoveUncompletedWatchesAsync(uint256.Zero, CancellationToken.None);
            Thread.Sleep(TimeSpan.FromMilliseconds(1000));

            // Assert.
            _ = this.callbackExecuter.Received(1).ExecuteAsync
            (
                rule.Callback.Id,
                rule.Callback.Url,
                Arg.Is<CallbackResult>
                (
                    result => result.Status == CallbackResult.StatusError
                ),
                Arg.Any<CancellationToken>()
            );

            _ = this.ruleRepository.Received(1).UpdateCurrentWatchAsync(
                Arg.Is<Guid>(id => id == rule.Id), Arg.Is<Guid?>(id => id == null), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task RemoveUncompletedWatchesAsync_WithRemainingTimeout_ShouldNotExecuteTimeoutImmediately()
        {
            // Arrange.
            var builder = new WatchArgsBuilder(this.callbackRepository);

            builder.Timeout = TimeSpan.FromMilliseconds(500);
            builder.Confirmations = 10;
            var watch = await builder.Call(this.subject.AddTransactionAsync);

            var watches = new List<TransactionWatch<Rule>>()
            {
                new TransactionWatch<Rule>(watch, uint256.Zero, builder.Transaction),
            };

            await this.handler.AddWatchesAsync(watches, CancellationToken.None);
            Thread.Sleep(TimeSpan.FromMilliseconds(1000));

            // Act & Assert.
            var updated = await this.ruleRepository.GetAsync(watch.Id, CancellationToken.None);
            Assert.True(await this.ruleRepository.GetRemainingWaitingTimeAsync(updated.Id, CancellationToken.None) < TimeSpan.FromSeconds(1));

            await this.handler.RemoveUncompletedWatchesAsync(uint256.Zero, CancellationToken.None);

            _ = this.callbackExecuter.Received(0).ExecuteAsync
            (
                Arg.Any<Guid>(),
                Arg.Any<Uri>(),
                Arg.Any<CallbackResult>(),
                Arg.Any<CancellationToken>()
            );

            Thread.Sleep(TimeSpan.FromMilliseconds(1000));

            _ = this.callbackExecuter.Received(1).ExecuteAsync
            (
                Arg.Any<Guid>(),
                Arg.Any<Uri>(),
                Arg.Is<CallbackResult>
                (
                    result => result.Status == CallbackResult.StatusError
                ),
                Arg.Any<CancellationToken>()
            );

            _ = this.ruleRepository.Received(1).UpdateCurrentWatchAsync(
                Arg.Is<Guid>(id => id == watch.Id), Arg.Is<Guid?>(id => id == null), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task RemoveCompletedWatchesAsync_WithExistKey_ShouldSuccess()
        {
            // Arrange.
            var builder = new WatchArgsBuilder(this.callbackRepository);

            builder.Timeout = TimeSpan.FromSeconds(1);
            builder.Confirmations = 10;
            var watch = await builder.Call(this.subject.AddTransactionAsync);

            var watches = new List<TransactionWatch<Rule>>()
            {
                new TransactionWatch<Rule>(watch, uint256.Zero, builder.Transaction),
            };

            await this.handler.AddWatchesAsync(watches, CancellationToken.None);

            // Act.
            await this.handler.RemoveCompletedWatchesAsync(watches, CancellationToken.None);

            // Assert.
            Assert.Empty(await this.handler.GetCurrentWatchesAsync(CancellationToken.None));
            _ = this.ruleRepository.Received(1).UpdateCurrentWatchAsync(
                Arg.Is<Guid>(id => id == watch.Id), Arg.Is<Guid?>(id => id == null), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Initialize_WithNonEmptyRepository_ShouldInitializeWatches()
        {
            // Arrange.
            var tx = Transaction.Parse(TestTransaction.Raw1, ZcoinNetworks.Instance.Mainnet);

            var builder = new WatchArgsBuilder(this.callbackRepository);
            builder.Timeout = TimeSpan.FromSeconds(1);
            builder.Transaction = tx.GetHash();

            var callback = await this.callbackRepository.AddAsync
            (
                builder.Ip, builder.CallbackUrl, CancellationToken.None
            );

            _ = await this.ruleRepository.AddAsync
            (
                builder.Transaction, builder.Confirmations, builder.Timeout, builder.SuccessData,
                builder.TimeoutData, callback, CancellationToken.None
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

            var watch = await this.ruleRepository.AddAsync
            (
                builder.Transaction, builder.Confirmations, builder.Timeout, builder.SuccessData,
                builder.TimeoutData, completedCallback, CancellationToken.None
            );

            await this.ruleRepository.UpdateStatusAsync(watch.Id, RuleStatus.Success, CancellationToken.None);

            // Act.
            ITransactionConfirmationWatcherHandler<Rule> localHandler;
            TransactionConfirmationWatcher localWatcher;

            localHandler = localWatcher = new TransactionConfirmationWatcher
            (
                this.callbackRepository,
                this.ruleRepository,
                this.blockStorage,
                this.callbackExecuter,
                this.watchRepository,
                this.logger
            );

            await localWatcher.StartAsync(CancellationToken.None);
            var retrievedCount = (await localHandler.CreateContextsAsync(tx, CancellationToken.None)).Count();
            Thread.Sleep(TimeSpan.FromSeconds(2));

            // Assert.
            Assert.Equal(1, retrievedCount);

            _ = this.callbackExecuter.Received(1).ExecuteAsync
            (
                Arg.Any<Guid>(),
                Arg.Any<Uri>(),
                Arg.Is<CallbackResult>
                (
                    result => result.Status == CallbackResult.StatusError
                ),
                Arg.Any<CancellationToken>()
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

        class WatchArgsBuilder
        {
            readonly ICallbackRepository CallbackRepository;

            public WatchArgsBuilder(ICallbackRepository callbackRepository)
            {
                this.CallbackRepository = callbackRepository;

                this.Transaction = uint256.Parse("7396ddaa275ed5492564277efc0844b4aeaa098020bc8d4b4dbc489134e49afd");
                this.Confirmations = 10;
                this.Timeout = TimeSpan.FromSeconds(1);
                this.Ip = IPAddress.Loopback;
                this.CallbackUrl = new Uri("http://zcoin.io");
                this.SuccessData = new CallbackResult(CallbackResult.StatusSuccess, "success");
                this.TimeoutData = new CallbackResult(CallbackResult.StatusError, "timeout");
                this.CancellationToken = CancellationToken.None;
            }

            public uint256 Transaction { get; set; }
            public int Confirmations { get; set; }
            public TimeSpan Timeout { get; set; }
            public IPAddress Ip { get; set; }
            public Uri CallbackUrl { get; set; }
            public CallbackResult SuccessData { get; set; }
            public CallbackResult TimeoutData { get; set; }
            public CancellationToken CancellationToken { get; set; }

            public async Task<T> Call<T>(Func<uint256, int, TimeSpan, Callback,
                CallbackResult,
                CallbackResult,
                CancellationToken, Task<T>> func)
            {
                var callback = await this.CallbackRepository.AddAsync(
                    this.Ip,
                    this.CallbackUrl,
                    CancellationToken.None);

                return await func(Transaction, Confirmations, Timeout, callback,
                    SuccessData, TimeoutData, CancellationToken);
            }
        }
    }
}
