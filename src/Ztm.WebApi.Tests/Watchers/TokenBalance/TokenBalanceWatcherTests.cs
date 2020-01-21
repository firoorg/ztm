using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NBitcoin;
using Xunit;
using Ztm.Testing;
using Ztm.WebApi.Callbacks;
using Ztm.WebApi.Watchers.TokenBalance;
using Ztm.Zcoin.NBitcoin.Exodus;
using Ztm.Zcoin.NBitcoin.Exodus.TransactionRetrievers;
using Ztm.Zcoin.Synchronization;
using Ztm.Zcoin.Watching;
using Confirmation = Ztm.Zcoin.Watching.BalanceConfirmation<Ztm.WebApi.Watchers.TokenBalance.Rule, Ztm.Zcoin.NBitcoin.Exodus.PropertyAmount>;
using Watch = Ztm.Zcoin.Watching.BalanceWatch<Ztm.WebApi.Watchers.TokenBalance.Rule, Ztm.Zcoin.NBitcoin.Exodus.PropertyAmount>;

namespace Ztm.WebApi.Tests.Watchers.TokenBalance
{
    public sealed class TokenBalanceWatcherTests : IDisposable
    {
        readonly PropertyId property;
        readonly Block block1;
        readonly Block block2;
        readonly Block block3;
        readonly Block block4;
        readonly Callback callback1;
        readonly Callback callback2;
        readonly Rule rule1;
        readonly Rule rule2;
        readonly Mock<ILogger<TokenBalanceWatcher>> logger;
        readonly Mock<IBlocksStorage> blocks;
        readonly Mock<ITransactionRetriever> exodusRetriever;
        readonly Mock<IRuleRepository> rules;
        readonly Mock<IWatchRepository> watches;
        readonly Mock<ICallbackRepository> callbacks;
        readonly Mock<ICallbackExecuter> callbackExecutor;
        readonly FakeTimerScheduler timerScheduler;
        readonly TokenBalanceWatcher subject;

        public TokenBalanceWatcherTests()
        {
            this.property = new PropertyId(3);

            this.block1 = TestBlock.Regtest0;
            this.block2 = TestBlock.Regtest1;
            this.block3 = TestBlock.Regtest2;
            this.block4 = TestBlock.Regtest3;

            this.callback1 = new Callback(
                Guid.NewGuid(),
                IPAddress.Parse("192.168.1.2"),
                DateTime.Now,
                false,
                new Uri("http://localhost/a"));
            this.callback2 = new Callback(
                Guid.NewGuid(),
                IPAddress.Parse("192.168.1.3"),
                DateTime.Now,
                false,
                new Uri("http://localhost/b"));

            this.rule1 = new Rule(
                this.property,
                TestAddress.Regtest1,
                new PropertyAmount(100),
                3,
                TimeSpan.FromHours(1),
                "timeout",
                this.callback1.Id);
            this.rule2 = new Rule(
                this.property,
                TestAddress.Regtest2,
                new PropertyAmount(50),
                1,
                TimeSpan.FromMinutes(30),
                "watch-timeout",
                this.callback2.Id);

            this.logger = new Mock<ILogger<TokenBalanceWatcher>>();

            this.blocks = new Mock<IBlocksStorage>();
            this.blocks
                .Setup(r => r.GetAsync(this.block1.GetHash(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((this.block1, 0));
            this.blocks
                .Setup(r => r.GetAsync(this.block2.GetHash(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((this.block2, 1));
            this.blocks
                .Setup(r => r.GetAsync(this.block3.GetHash(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((this.block3, 2));
            this.blocks
                .Setup(r => r.GetAsync(this.block4.GetHash(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((this.block4, 3));

            this.exodusRetriever = new Mock<ITransactionRetriever>();
            this.exodusRetriever
                .Setup(r => r.GetBalanceChangesAsync(this.block1.Transactions[0], It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[]
                {
                    new BalanceChange(
                        TestAddress.Regtest3,
                        -(this.rule1.TargetAmount + this.rule2.TargetAmount),
                        this.property),
                    new BalanceChange(
                        this.rule1.Address,
                        this.rule1.TargetAmount + this.rule2.TargetAmount,
                        this.property),
                });
            this.exodusRetriever
                .Setup(r => r.GetBalanceChangesAsync(this.block2.Transactions[0], It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[]
                {
                    new BalanceChange(this.rule1.Address, -(this.rule2.TargetAmount / 2), this.property),
                    new BalanceChange(this.rule2.Address, this.rule2.TargetAmount / 2, this.property),
                });
            this.exodusRetriever
                .Setup(r => r.GetBalanceChangesAsync(this.block3.Transactions[0], It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[]
                {
                    new BalanceChange(TestAddress.Regtest3, -(this.rule2.TargetAmount / 2), this.property),
                    new BalanceChange(this.rule2.Address, this.rule2.TargetAmount / 2, this.property),
                });

            this.rules = new Mock<IRuleRepository>();

            this.watches = new Mock<IWatchRepository>();

            this.callbacks = new Mock<ICallbackRepository>();
            this.callbacks
                .Setup(r => r.GetAsync(this.callback1.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.callback1);
            this.callbacks
                .Setup(r => r.GetAsync(this.callback2.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.callback2);

            this.callbackExecutor = new Mock<ICallbackExecuter>();

            this.timerScheduler = new FakeTimerScheduler();

            this.subject = new TokenBalanceWatcher(
                this.property,
                this.logger.Object,
                this.blocks.Object,
                this.exodusRetriever.Object,
                this.rules.Object,
                this.watches.Object,
                this.callbacks.Object,
                this.callbackExecutor.Object,
                this.timerScheduler);
        }

        public void Dispose()
        {
            this.subject.Dispose();
        }

        [Fact]
        public void Constructor_WithNullProperty_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "property",
                () => new TokenBalanceWatcher(
                    null,
                    this.logger.Object,
                    this.blocks.Object,
                    this.exodusRetriever.Object,
                    this.rules.Object,
                    this.watches.Object,
                    this.callbacks.Object,
                    this.callbackExecutor.Object,
                    this.timerScheduler));
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "logger",
                () => new TokenBalanceWatcher(
                    this.property,
                    null,
                    this.blocks.Object,
                    this.exodusRetriever.Object,
                    this.rules.Object,
                    this.watches.Object,
                    this.callbacks.Object,
                    this.callbackExecutor.Object,
                    this.timerScheduler));
        }

        [Fact]
        public void Constructor_WithNullExodusRetriever_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "exodusRetriever",
                () => new TokenBalanceWatcher(
                    this.property,
                    this.logger.Object,
                    this.blocks.Object,
                    null,
                    this.rules.Object,
                    this.watches.Object,
                    this.callbacks.Object,
                    this.callbackExecutor.Object,
                    this.timerScheduler));
        }

        [Fact]
        public void Constructor_WithNullRules_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "rules",
                () => new TokenBalanceWatcher(
                    this.property,
                    this.logger.Object,
                    this.blocks.Object,
                    this.exodusRetriever.Object,
                    null,
                    this.watches.Object,
                    this.callbacks.Object,
                    this.callbackExecutor.Object,
                    this.timerScheduler));
        }

        [Fact]
        public void Constructor_WithNullWatches_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "watches",
                () => new TokenBalanceWatcher(
                    this.property,
                    this.logger.Object,
                    this.blocks.Object,
                    this.exodusRetriever.Object,
                    this.rules.Object,
                    null,
                    this.callbacks.Object,
                    this.callbackExecutor.Object,
                    this.timerScheduler));
        }

        [Fact]
        public void Constructor_WithNullCallbacks_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "callbacks",
                () => new TokenBalanceWatcher(
                    this.property,
                    this.logger.Object,
                    this.blocks.Object,
                    this.exodusRetriever.Object,
                    this.rules.Object,
                    this.watches.Object,
                    null,
                    this.callbackExecutor.Object,
                    this.timerScheduler));
        }

        [Fact]
        public void Constructor_WithNullCallbackExecutor_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "callbackExecutor",
                () => new TokenBalanceWatcher(
                    this.property,
                    this.logger.Object,
                    this.blocks.Object,
                    this.exodusRetriever.Object,
                    this.rules.Object,
                    this.watches.Object,
                    this.callbacks.Object,
                    null,
                    this.timerScheduler));
        }

        [Fact]
        public void Constructor_WithNullTimerScheduler_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "timerScheduler",
                () => new TokenBalanceWatcher(
                    this.property,
                    this.logger.Object,
                    this.blocks.Object,
                    this.exodusRetriever.Object,
                    this.rules.Object,
                    this.watches.Object,
                    this.callbacks.Object,
                    this.callbackExecutor.Object,
                    null));
        }

        [Fact]
        public void Dispose_MultipleCall_ShouldSuccess()
        {
            this.subject.Dispose();
        }

        [Fact]
        public Task StartAsync_WhenInvoke_ShouldStartTimersForAllUncompletedRules()
        {
            return AsynchronousTesting.WithCancellationTokenAsync(async cancellationToken =>
            {
                // Arrange.
                this.rules
                    .Setup(r => r.ListUncompletedAsync(this.property, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new[] { this.rule1 });
                this.rules
                    .Setup(r => r.GetCurrentTimeoutAsync(this.rule1.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(this.rule1.OriginalTimeout);

                // Act.
                await this.subject.StartAsync(cancellationToken);

                // Assert.
                var schedule = Assert.Single(this.timerScheduler.ActiveSchedules);

                Assert.Equal(this.rule1.OriginalTimeout, schedule.Due);
                Assert.Null(schedule.Period);
                Assert.NotNull(schedule.Handler);
                Assert.Equal(this.rule1.Address, schedule.Context);

                this.rules.Verify(
                    r => r.ListUncompletedAsync(this.property, cancellationToken),
                    Times.Once());

                this.rules.Verify(
                    r => r.GetCurrentTimeoutAsync(this.rule1.Id, CancellationToken.None),
                    Times.Once());
            });
        }

        [Fact]
        public Task StartWatchAsync_WithUnsupportedTimeout_ShouldThrow()
        {
            this.timerScheduler.DurationValidator = duration => duration == this.rule1.OriginalTimeout;

            return Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                "timeout",
                () => this.subject.StartWatchAsync(
                    this.rule1.Address,
                    this.rule1.TargetAmount,
                    this.rule1.TargetConfirmation,
                    TimeSpan.Zero,
                    this.rule1.TimeoutStatus,
                    this.rule1.Callback,
                    CancellationToken.None));
        }

        [Fact]
        public Task StartWatchAsync_WithInvalidCallback_ShouldThrow()
        {
            return AsynchronousTesting.WithCancellationTokenAsync(async cancellationToken =>
            {
                // Arrange
                var callback = Guid.NewGuid();

                // Act.
                await Assert.ThrowsAsync<ArgumentException>(
                    "callback",
                    () => this.subject.StartWatchAsync(
                        this.rule1.Address,
                        this.rule1.TargetAmount,
                        this.rule1.TargetConfirmation,
                        this.rule1.OriginalTimeout,
                        this.rule1.TimeoutStatus,
                        callback,
                        cancellationToken));

                // Assert.
                this.callbacks.Verify(
                    r => r.GetAsync(callback, cancellationToken),
                    Times.Once());
            });
        }

        [Fact]
        public Task StartWatchAsync_NotStarted_ShouldThrow()
        {
            return Assert.ThrowsAsync<InvalidOperationException>(
                () => this.subject.StartWatchAsync(
                    this.rule1.Address,
                    this.rule1.TargetAmount,
                    this.rule1.TargetConfirmation,
                    this.rule1.OriginalTimeout,
                    this.rule1.TimeoutStatus,
                    this.rule1.Callback,
                    CancellationToken.None));
        }

        [Fact]
        public async Task StartWatchAsync_AlreadyStopped_ShouldThrow()
        {
            // Arrange.
            await StartSubjectAsync();
            await this.subject.StopAsync(CancellationToken.None);

            // Act.
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => this.subject.StartWatchAsync(
                    this.rule1.Address,
                    this.rule1.TargetAmount,
                    this.rule1.TargetConfirmation,
                    this.rule1.OriginalTimeout,
                    this.rule1.TimeoutStatus,
                    this.rule1.Callback,
                    CancellationToken.None));
        }

        [Fact]
        public Task StartWatchAsync_WithValidArgs_ShouldStartWatchingThatAddress()
        {
            return AsynchronousTesting.WithCancellationTokenAsync(async cancellationToken =>
            {
                // Arrange.
                await StartSubjectAsync();

                // Act.
                var result = await InvokeStartWatchAsync(this.rule1, cancellationToken);

                // Assert.
                var schedule = Assert.Single(this.timerScheduler.ActiveSchedules);

                Assert.Equal(this.rule1.Address, schedule.Context);
                Assert.Equal(this.rule1.OriginalTimeout, schedule.Due);
                Assert.Null(schedule.Period);

                Assert.Equal(this.rule1.Address, result.Address);
                Assert.Equal(this.rule1.Callback, result.Callback);
                Assert.Equal(this.rule1.OriginalTimeout, result.OriginalTimeout);
                Assert.Equal(this.rule1.Property, result.Property);
                Assert.Equal(this.rule1.TargetAmount, result.TargetAmount);
                Assert.Equal(this.rule1.TargetConfirmation, result.TargetConfirmation);
                Assert.Equal(this.rule1.TimeoutStatus, result.TimeoutStatus);

                this.rules.Verify(
                    r => r.AddAsync(result, cancellationToken),
                    Times.Once());
            });
        }

        [Fact]
        public async Task StartWatchAsync_WatchingHaveBeenTimeoutAndItNotCompletedYet_ShouldRaiseTimeoutCallback()
        {
            // Arrange.
            await StartSubjectAsync();

            var rule = await InvokeStartWatchAsync(this.rule1, CancellationToken.None);

            this.watches
                .Setup(r => r.TransitionToTimedOutAsync(rule, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[]
                {
                    new CompletedWatch(
                        new Watch(
                            rule,
                            this.block1.GetHash(),
                            this.block1.Transactions[0].GetHash(),
                            rule.Address,
                            this.rule1.TargetAmount + this.rule2.TargetAmount),
                        1),
                    new CompletedWatch(
                        new Watch(
                            rule,
                            this.block2.GetHash(),
                            this.block2.Transactions[0].GetHash(),
                            rule.Address,
                            -(this.rule2.TargetAmount / 2)),
                        0)
                });

            // Act.
            this.timerScheduler.Trigger(i => i.Context.Equals(rule.Address));
            await this.subject.StopAsync(CancellationToken.None);

            // Assert.
            var expect = new CallbackResult(
                rule.TimeoutStatus,
                new TimeoutData()
                {
                    Received = this.rule1.TargetAmount + this.rule2.TargetAmount,
                    Confirmation = 1,
                    TargetConfirmation = rule.TargetConfirmation,
                });

            this.rules.Verify(
                r => r.SetTimedOutAsync(rule.Id, CancellationToken.None),
                Times.Once());

            this.watches.Verify(
                r => r.TransitionToTimedOutAsync(rule, CancellationToken.None),
                Times.Once());

            this.callbacks.Verify(
                r => r.AddHistoryAsync(this.callback1.Id, expect, CancellationToken.None),
                Times.Once());

            this.callbackExecutor.Verify(
                e => e.ExecuteAsync(this.callback1.Id, this.callback1.Url, expect, CancellationToken.None),
                Times.Once());

            this.callbacks.Verify(
                r => r.SetCompletedAsyc(this.callback1.Id, CancellationToken.None),
                Times.Once());

            this.rules.Verify(
                r => r.DecreaseTimeoutAsync(rule.Id, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
                Times.Never());
        }

        [Fact]
        public Task StartWatchAsync_WatchingHaveBeenTimeoutButItAlreadyCompleted_ShouldDoNothing()
        {
            return AsynchronousTesting.WithCancellationTokenAsync(async cancellationToken =>
            {
                // Arrange.
                await StartSubjectAsync();

                var rule = await InvokeStartWatchAsync(this.rule1, CancellationToken.None);

                var confirm = new Confirmation(this.block4.GetHash(), rule.Address, new Dictionary<Watch, int>()
                {
                    {
                        new Watch(
                            rule,
                            this.block1.GetHash(),
                            this.block1.Transactions[0].GetHash(),
                            rule.Address,
                            rule.TargetAmount + this.rule2.TargetAmount),
                        4
                    },
                    {
                        new Watch(
                            rule,
                            this.block2.GetHash(),
                            this.block2.Transactions[0].GetHash(),
                            rule.Address,
                            -(this.rule2.TargetAmount / 2)),
                        3
                    }
                });

                await InvokeConfirmationUpdateAsync(
                    confirm,
                    confirm.Watches.Min(p => p.Value),
                    ConfirmationType.Confirmed,
                    cancellationToken);

                // Act.
                this.timerScheduler.Trigger(s => s.Context.Equals(rule.Address));

                // Assert.
                this.rules.Verify(
                    r => r.SetSucceededAsync(rule.Id, CancellationToken.None),
                    Times.Once());

                this.rules.Verify(
                    r => r.SetTimedOutAsync(rule.Id, It.IsAny<CancellationToken>()),
                    Times.Never());

                this.watches.Verify(
                    r => r.TransitionToTimedOutAsync(rule, It.IsAny<CancellationToken>()),
                    Times.Never());

                this.callbacks.Verify(
                    r => r.AddHistoryAsync(
                        this.callback1.Id,
                        It.Is<CallbackResult>(cr => cr.Status != CallbackResult.StatusSuccess),
                        It.IsAny<CancellationToken>()),
                    Times.Never());

                this.callbackExecutor.Verify(
                    e => e.ExecuteAsync(
                        this.callback1.Id,
                        It.IsAny<Uri>(),
                        It.Is<CallbackResult>(r => r.Status != CallbackResult.StatusSuccess),
                        It.IsAny<CancellationToken>()),
                    Times.Never());

                this.callbacks.Verify(
                    r => r.SetCompletedAsyc(this.callback1.Id, It.IsAny<CancellationToken>()),
                    Times.Once());
            });
        }

        [Fact]
        public async Task StartWatchAsync_WatchingHaveBeenTimeoutButThereIsException_ShouldLogThatException()
        {
            // Arrange.
            await StartSubjectAsync();

            var rule = await InvokeStartWatchAsync(this.rule1, CancellationToken.None);

            this.rules
                .Setup(r => r.SetTimedOutAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception());

            // Act.
            this.timerScheduler.Trigger(i => i.Context.Equals(rule.Address));
            await this.subject.StopAsync(CancellationToken.None);

            // Assert.
            this.rules.Verify(
                r => r.SetTimedOutAsync(rule.Id, CancellationToken.None),
                Times.Once());

            this.logger.Verify(
                l => l.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsNotNull<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once());

            this.rules.Verify(
                r => r.DecreaseTimeoutAsync(rule.Id, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
                Times.Never());
        }

        [Fact]
        public Task StopAsync_WithActiveTimers_ShouldStopItAndDecreaseTimeout()
        {
            return AsynchronousTesting.WithCancellationTokenAsync(async cancellationToken =>
            {
                // Arrange.
                await StartSubjectAsync(this.rule1, this.rule2);

                this.timerScheduler.Trigger(s => s.Context.Equals(this.rule1.Address));

                // Act.
                await this.subject.StopAsync(cancellationToken);

                // Assert.
                this.rules.Verify(
                    r => r.DecreaseTimeoutAsync(
                        this.rule2.Id,
                        It.Is<TimeSpan>(v => v > TimeSpan.Zero),
                        CancellationToken.None),
                    Times.Once());

                var schedule = Assert.Single(
                    this.timerScheduler.StoppedSchedules,
                    s => s.Context.Equals(this.rule2.Address));

                Assert.Equal(this.rule2.OriginalTimeout, schedule.Due);
                Assert.Null(schedule.Period);
            });
        }

        [Fact]
        public Task AddWatchesAsync_WithNonEmptyWatches_ShouldStoreOnlyWatchesForActiveRule()
        {
            return AsynchronousTesting.WithCancellationTokenAsync(async cancellationToken =>
            {
                // Arrange.
                var amount = this.rule2.TargetAmount / 2;
                var watch1 = new Watch(
                    this.rule1,
                    this.block2.GetHash(),
                    this.block2.Transactions[0].GetHash(),
                    this.rule1.Address,
                    -amount);
                var watch2 = new Watch(
                    this.rule2,
                    this.block2.GetHash(),
                    this.block2.Transactions[0].GetHash(),
                    this.rule2.Address,
                    amount);

                await StartSubjectAsync(this.rule1);

                // Act.
                await InvokeAddWatchesAsync(new[] { watch1, watch2 }, cancellationToken);

                // Assert.
                this.watches.Verify(
                    r => r.AddAsync(It.Is<IEnumerable<Watch>>(l => l.Single().Id == watch1.Id), cancellationToken),
                    Times.Once());
            });
        }

        [Fact]
        public async Task ConfirmationUpdateAsync_WatchingHaveBeenTimeout_ShouldDoNothing()
        {
            // Arrange.
            await StartSubjectAsync(this.rule1);

            this.timerScheduler.Trigger(i => i.Context.Equals(this.rule1.Address));

            var confirm = new Confirmation(
                this.block1.GetHash(),
                this.rule1.Address,
                new Dictionary<Watch, int>()
                {
                    {
                        new Watch(
                            this.rule1,
                            this.block1.GetHash(),
                            this.block1.Transactions[0].GetHash(),
                            this.rule1.Address,
                            this.rule1.TargetAmount + this.rule2.TargetAmount),
                        1
                    }
                });

            // Act.
            var result = await InvokeConfirmationUpdateAsync(confirm, 1, ConfirmationType.Confirmed, CancellationToken.None);

            // Assert.
            Assert.False(result);

            this.rules.Verify(
                r => r.SetTimedOutAsync(this.rule1.Id, CancellationToken.None),
                Times.Once());

            this.watches.Verify(
                r => r.SetConfirmationCountAsync(
                    It.IsAny<IReadOnlyDictionary<Watch, int>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never());

            this.rules.Verify(
                r => r.SetSucceededAsync(this.rule1.Id, It.IsAny<CancellationToken>()),
                Times.Never());

            this.callbacks.Verify(
                r => r.AddHistoryAsync(
                    this.callback1.Id,
                    It.Is<CallbackResult>(cr => cr.Status == CallbackResult.StatusSuccess),
                    It.IsAny<CancellationToken>()),
                Times.Never());

            this.callbackExecutor.Verify(
                e => e.ExecuteAsync(
                    this.callback1.Id,
                    It.IsAny<Uri>(),
                    It.Is<CallbackResult>(cr => cr.Status == CallbackResult.StatusSuccess),
                    It.IsAny<CancellationToken>()),
                Times.Never());

            this.callbacks.Verify(
                r => r.SetCompletedAsyc(this.callback1.Id, It.IsAny<CancellationToken>()),
                Times.Once());
        }

        [Fact]
        public Task ConfirmationUpdateAsync_ConfirmationIsNotEnough_ShouldNotComplete()
        {
            return AsynchronousTesting.WithCancellationTokenAsync(async cancellationToken =>
            {
                // Arrange.
                await StartSubjectAsync(this.rule1);

                var watches = new Dictionary<Watch, int>()
                {
                    {
                        new Watch(
                            this.rule1,
                            this.block1.GetHash(),
                            this.block1.Transactions[0].GetHash(),
                            this.rule1.Address,
                            this.rule1.TargetAmount + this.rule2.TargetAmount),
                        1
                    }
                };

                var confirm = new Confirmation(this.block1.GetHash(), this.rule1.Address, watches);

                // Act.
                var result = await InvokeConfirmationUpdateAsync(confirm, 1, ConfirmationType.Confirmed, cancellationToken);

                // Assert.
                Assert.False(result);

                this.watches.Verify(
                    r => r.SetConfirmationCountAsync(watches, cancellationToken),
                    Times.Once());

                this.rules.Verify(
                    r => r.SetSucceededAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                    Times.Never());

                this.callbacks.Verify(
                    r => r.AddHistoryAsync(It.IsAny<Guid>(), It.IsAny<CallbackResult>(), It.IsAny<CancellationToken>()),
                    Times.Never());

                this.callbackExecutor.Verify(
                    e => e.ExecuteAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<Uri>(),
                        It.IsAny<CallbackResult>(),
                        It.IsAny<CancellationToken>()),
                    Times.Never());

                this.callbacks.Verify(
                    r => r.SetCompletedAsyc(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                    Times.Never());

                Assert.Empty(this.timerScheduler.StoppedSchedules);

                await this.subject.StopAsync(CancellationToken.None);

                this.rules.Verify(
                    r => r.DecreaseTimeoutAsync(
                        this.rule1.Id,
                        It.Is<TimeSpan>(t => t > TimeSpan.Zero),
                        CancellationToken.None),
                    Times.Once());
            });
        }

        [Fact]
        public Task ConfirmationUpdateAsync_ReceivedIsNotEnough_ShouldNotComplete()
        {
            return AsynchronousTesting.WithCancellationTokenAsync(async cancellationToken =>
            {
                // Arrange.
                await StartSubjectAsync(this.rule2);

                var watches = new Dictionary<Watch, int>()
                {
                    {
                        new Watch(
                            this.rule2,
                            this.block2.GetHash(),
                            this.block2.Transactions[0].GetHash(),
                            this.rule2.Address,
                            this.rule2.TargetAmount / 2),
                        1
                    }
                };

                var confirm = new Confirmation(this.block2.GetHash(), this.rule2.Address, watches);

                // Act.
                var result = await InvokeConfirmationUpdateAsync(confirm, 1, ConfirmationType.Confirmed, cancellationToken);

                // Assert.
                Assert.False(result);

                this.watches.Verify(
                    r => r.SetConfirmationCountAsync(watches, cancellationToken),
                    Times.Once());

                this.rules.Verify(
                    r => r.SetSucceededAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                    Times.Never());

                this.callbacks.Verify(
                    r => r.AddHistoryAsync(It.IsAny<Guid>(), It.IsAny<CallbackResult>(), It.IsAny<CancellationToken>()),
                    Times.Never());

                this.callbackExecutor.Verify(
                    e => e.ExecuteAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<Uri>(),
                        It.IsAny<CallbackResult>(),
                        It.IsAny<CancellationToken>()),
                    Times.Never());

                this.callbacks.Verify(
                    r => r.SetCompletedAsyc(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                    Times.Never());

                Assert.Empty(this.timerScheduler.StoppedSchedules);

                await this.subject.StopAsync(CancellationToken.None);

                this.rules.Verify(
                    r => r.DecreaseTimeoutAsync(
                        this.rule2.Id,
                        It.Is<TimeSpan>(t => t > TimeSpan.Zero),
                        CancellationToken.None),
                    Times.Once());
            });
        }

        [Fact]
        public Task ConfirmationUpdateAsync_ConfirmationAndReceivedAreEnough_ShouldComplete()
        {
            return AsynchronousTesting.WithCancellationTokenAsync(async cancellationToken =>
            {
                // Arrange.
                await StartSubjectAsync(this.rule1);

                var watches = new Dictionary<Watch, int>()
                {
                    {
                        new Watch(
                            this.rule1,
                            this.block1.GetHash(),
                            this.block1.Transactions[0].GetHash(),
                            this.rule1.Address,
                            this.rule1.TargetAmount + this.rule2.TargetAmount),
                        4
                    },
                    {
                        new Watch(
                            this.rule1,
                            this.block2.GetHash(),
                            this.block2.Transactions[0].GetHash(),
                            this.rule1.Address,
                            -(this.rule2.TargetAmount / 2)),
                        3
                    }
                };

                var confirm = new Confirmation(this.block4.GetHash(), this.rule1.Address, watches);

                // Act.
                var result = await InvokeConfirmationUpdateAsync(confirm, 3, ConfirmationType.Confirmed, cancellationToken);

                // Assert.
                var expect = new CallbackResult(
                    CallbackResult.StatusSuccess,
                    new CallbackData()
                    {
                        Received = this.rule1.TargetAmount + this.rule2.TargetAmount / 2,
                        Confirmation = 3,
                    });

                Assert.True(result);

                this.watches.Verify(
                    r => r.SetConfirmationCountAsync(watches, cancellationToken),
                    Times.Once());

                this.rules.Verify(
                    r => r.SetSucceededAsync(this.rule1.Id, CancellationToken.None),
                    Times.Once());

                this.callbacks.Verify(
                    r => r.AddHistoryAsync(this.callback1.Id, expect, CancellationToken.None),
                    Times.Once());

                this.callbackExecutor.Verify(
                    e => e.ExecuteAsync(this.callback1.Id, this.callback1.Url, expect, CancellationToken.None),
                    Times.Once());

                this.callbacks.Verify(
                    r => r.SetCompletedAsyc(this.callback1.Id, CancellationToken.None),
                    Times.Once());

                var stopped = Assert.Single(this.timerScheduler.StoppedSchedules);

                Assert.Equal(this.rule1.Address, stopped.Context);
                Assert.Equal(this.rule1.OriginalTimeout, stopped.Due);
                Assert.NotNull(stopped.Handler);
                Assert.Null(stopped.Period);

                await this.subject.StopAsync(CancellationToken.None);

                this.rules.Verify(
                    r => r.DecreaseTimeoutAsync(It.IsAny<Guid>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
                    Times.Never());
            });
        }

        [Fact]
        public Task GetBalanceChangesAsync_WithNonExodusTx_ShouldReturnEmptyChanges()
        {
            return AsynchronousTesting.WithCancellationTokenAsync(async cancellationToken =>
            {
                // Arrange.
                var tx = this.block1.Transactions[0];

                this.exodusRetriever
                    .Setup(r => r.GetBalanceChangesAsync(tx, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((IEnumerable<BalanceChange>)null);

                // Act.
                var result = await InvokeGetBalanceChangesAsync(tx, cancellationToken);

                // Assert.
                Assert.Empty(result);

                this.exodusRetriever.Verify(
                    r => r.GetBalanceChangesAsync(tx, cancellationToken),
                    Times.Once());
            });
        }

        [Fact]
        public Task GetBalanceChangesAsync_WithExodusTxNotMatchedProperty_ShouldReturnEmptyChanges()
        {
            return AsynchronousTesting.WithCancellationTokenAsync(async cancellationToken =>
            {
                // Arrange.
                var property = new PropertyId(10);
                var tx = this.block1.Transactions[0];

                this.exodusRetriever
                    .Setup(r => r.GetBalanceChangesAsync(tx, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new[]
                    {
                        new BalanceChange(
                            TestAddress.Regtest3,
                            -(this.rule1.TargetAmount + this.rule2.TargetAmount),
                            property),
                        new BalanceChange(
                            this.rule1.Address,
                            this.rule1.TargetAmount + this.rule2.TargetAmount,
                            property),
                    });

                await StartSubjectAsync(this.rule1);

                // Act.
                var result = await InvokeGetBalanceChangesAsync(tx, cancellationToken);

                // Assert.
                Assert.Empty(result);

                this.exodusRetriever.Verify(
                    r => r.GetBalanceChangesAsync(tx, cancellationToken),
                    Times.Once());
            });
        }

        [Fact]
        public Task GetBalanceChangesAsync_WithExodusTxMatchedTargetProperty_ShouldReturnChangesForActiveWatch()
        {
            return AsynchronousTesting.WithCancellationTokenAsync(async cancellationToken =>
            {
                // Arrange.
                var tx = this.block2.Transactions[0];

                await StartSubjectAsync(this.rule1);

                // Act.
                var result = await InvokeGetBalanceChangesAsync(tx, cancellationToken);

                // Assert.
                var change = Assert.Single(result);

                Assert.Equal(this.rule1.Address, change.Key);
                Assert.Equal(-(this.rule2.TargetAmount / 2), change.Value.Amount);
                Assert.Equal(this.rule1, change.Value.Context);

                this.exodusRetriever.Verify(
                    r => r.GetBalanceChangesAsync(tx, cancellationToken),
                    Times.Once());
            });
        }

        [Fact]
        public Task GetCurrentWatchesAsync_WhenInvoke_ShouldReturnUncompletedWatches()
        {
            return AsynchronousTesting.WithCancellationTokenAsync(async cancellationToken =>
            {
                // Arrange.
                var watches = new[]
                {
                    new Watch(
                        this.rule1,
                        this.block1.GetHash(),
                        this.block1.Transactions[0].GetHash(),
                        this.rule1.Address,
                        this.rule1.TargetAmount + this.rule2.TargetAmount)
                };

                this.watches
                    .Setup(r => r.ListUncompletedAsync(this.property, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(watches);

                // Act.
                var result = await InvokeGetCurrentWatchesAsync(cancellationToken);

                // Assert.
                Assert.Equal(watches, result);

                this.watches.Verify(
                    r => r.ListUncompletedAsync(this.property, cancellationToken),
                    Times.Once());
            });
        }

        [Fact]
        public Task RemoveCompletedWatchesAsync_WhenInvoke_ShouldTransitionWatchesToSucceeded()
        {
            return AsynchronousTesting.WithCancellationTokenAsync(async cancellationToken =>
            {
                // Arrange.
                var watches = new[]
                {
                    new Watch(
                        this.rule1,
                        this.block1.GetHash(),
                        this.block1.Transactions[0].GetHash(),
                        this.rule1.Address,
                        this.rule1.TargetAmount + this.rule2.TargetAmount)
                };

                // Act.
                await InvokeRemoveCompletedWatchesAsync(watches, cancellationToken);

                // Assert.
                this.watches.Verify(
                    r => r.TransitionToSucceededAsync(watches, cancellationToken),
                    Times.Once());
            });
        }

        [Fact]
        public Task RemoveUncompletedWatchesAsync_WhenInvoke_ShouldTransitionAllMatchedWatchesToRejected()
        {
            return AsynchronousTesting.WithCancellationTokenAsync(async cancellationToken =>
            {
                // Act.
                await InvokeRemoveUncompletedWatchesAsync(this.block1.GetHash(), cancellationToken);

                // Assert.
                this.watches.Verify(
                    r => r.TransitionToRejectedAsync(this.property, this.block1.GetHash(), cancellationToken),
                    Times.Once());
            });
        }

        [Fact]
        public Task BlockAddedAsync_WhenInvoke_ShouldExecuteEngineWithBlockAddedEvent()
        {
            return AsynchronousTesting.WithCancellationTokenAsync(async cancellationToken =>
            {
                // Act.
                await InvokeBlockAddedAsync(this.block1, 0, cancellationToken);

                // Assert.
                this.exodusRetriever.Verify(
                    r => r.GetBalanceChangesAsync(this.block1.Transactions[0], cancellationToken),
                    Times.Once());

                this.watches.Verify(
                    r => r.ListUncompletedAsync(this.property, cancellationToken),
                    Times.Once());
            });
        }

        [Fact]
        public Task BlockRemovingAsync_WhenInvoke_ShouldExecuteEngineWithBlockRemoving()
        {
            return AsynchronousTesting.WithCancellationTokenAsync(async cancellationToken =>
            {
                // Act.
                await InvokeBlockRemovingAsync(this.block1, 0, cancellationToken);

                // Assert.
                this.watches.Verify(
                    r => r.ListUncompletedAsync(this.property, cancellationToken),
                    Times.Once());

                this.watches.Verify(
                    r => r.TransitionToRejectedAsync(this.property, this.block1.GetHash(), CancellationToken.None),
                    Times.Once());
            });
        }

        Task StartSubjectAsync(params Rule[] rules)
        {
            return StartSubjectAsync(rules.AsEnumerable());
        }

        Task StartSubjectAsync(IEnumerable<Rule> rules)
        {
            this.rules
                .Setup(r => r.ListUncompletedAsync(this.property, It.IsAny<CancellationToken>()))
                .ReturnsAsync(rules);

            foreach (var rule in rules)
            {
                this.rules
                    .Setup(r => r.GetCurrentTimeoutAsync(rule.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(rule.OriginalTimeout);
            }

            return this.subject.StartAsync(CancellationToken.None);
        }

        Task<Rule> InvokeStartWatchAsync(Rule rule, CancellationToken cancellationToken)
        {
            return this.subject.StartWatchAsync(
                rule.Address,
                rule.TargetAmount,
                rule.TargetConfirmation,
                rule.OriginalTimeout,
                rule.TimeoutStatus,
                rule.Callback,
                cancellationToken);
        }

        Task InvokeAddWatchesAsync(IEnumerable<Watch> watches, CancellationToken cancellationToken)
        {
            return ((IWatcherHandler<Rule, Watch>)this.subject).AddWatchesAsync(watches, cancellationToken);
        }

        Task<bool> InvokeConfirmationUpdateAsync(
            Confirmation confirm,
            int count,
            ConfirmationType type,
            CancellationToken cancellationToken)
        {
            return ((IConfirmationWatcherHandler<Rule, Watch, Confirmation>)this.subject).ConfirmationUpdateAsync(
                confirm,
                count,
                type,
                cancellationToken);
        }

        Task<IReadOnlyDictionary<BitcoinAddress, Ztm.Zcoin.Watching.BalanceChange<Rule, PropertyAmount>>> InvokeGetBalanceChangesAsync(
            Transaction tx,
            CancellationToken cancellationToken)
        {
            return ((IBalanceWatcherHandler<Rule, PropertyAmount>)this.subject).GetBalanceChangesAsync(tx, cancellationToken);
        }

        Task<IEnumerable<Watch>> InvokeGetCurrentWatchesAsync(CancellationToken cancellationToken)
        {
            return ((IConfirmationWatcherHandler<Rule, Watch, Confirmation>)this.subject).GetCurrentWatchesAsync(
                cancellationToken);
        }

        Task InvokeRemoveCompletedWatchesAsync(IEnumerable<Watch> watches, CancellationToken cancellationToken)
        {
            return ((IWatcherHandler<Rule, Watch>)this.subject).RemoveCompletedWatchesAsync(watches, cancellationToken);
        }

        Task InvokeRemoveUncompletedWatchesAsync(uint256 startedBlock, CancellationToken cancellationToken)
        {
            return ((IWatcherHandler<Rule, Watch>)this.subject).RemoveUncompletedWatchesAsync(
                startedBlock,
                cancellationToken);
        }

        Task InvokeBlockAddedAsync(Block block, int height, CancellationToken cancellationToken)
        {
            return ((IBlockListener)this.subject).BlockAddedAsync(block, height, cancellationToken);
        }

        Task InvokeBlockRemovingAsync(Block block, int height, CancellationToken cancellationToken)
        {
            return ((IBlockListener)this.subject).BlockRemovingAsync(block, height, cancellationToken);
        }
    }
}
