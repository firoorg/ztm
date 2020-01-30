using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NBitcoin;
using Xunit;
using Ztm.Testing;
using Ztm.WebApi.AddressPools;
using Ztm.WebApi.Callbacks;
using Ztm.WebApi.Watchers.TokenReceiving;
using Ztm.Zcoin.NBitcoin.Exodus;
using Ztm.Zcoin.NBitcoin.Exodus.TransactionRetrievers;
using Ztm.Zcoin.Synchronization;
using Ztm.Zcoin.Watching;
using Confirmation = Ztm.Zcoin.Watching.BalanceConfirmation<Ztm.WebApi.Watchers.TokenReceiving.Rule, Ztm.Zcoin.NBitcoin.Exodus.PropertyAmount>;
using Watch = Ztm.Zcoin.Watching.BalanceWatch<Ztm.WebApi.Watchers.TokenReceiving.Rule, Ztm.Zcoin.NBitcoin.Exodus.PropertyAmount>;

namespace Ztm.WebApi.Tests.Watchers.TokenReceiving
{
    public sealed class TokenReceivingWatcherTests : IDisposable
    {
        readonly PropertyId property;
        readonly Block block1;
        readonly Block block2;
        readonly Block block3;
        readonly Block block4;
        readonly ReceivingAddress address1;
        readonly ReceivingAddress address2;
        readonly ReceivingAddressReservation reservation1;
        readonly ReceivingAddressReservation reservation2;
        readonly Callback callback1;
        readonly Callback callback2;
        readonly Rule rule1;
        readonly Rule rule2;
        readonly Mock<ILogger<TokenReceivingWatcher>> logger;
        readonly Mock<IBlocksStorage> blocks;
        readonly Mock<IReceivingAddressPool> addressPool;
        readonly Mock<ITransactionRetriever> exodusRetriever;
        readonly Mock<IRuleRepository> rules;
        readonly Mock<IWatchRepository> watches;
        readonly Mock<ICallbackRepository> callbacks;
        readonly Mock<ICallbackExecuter> callbackExecutor;
        readonly FakeTimerScheduler timerScheduler;
        readonly TokenReceivingWatcher subject;

        public TokenReceivingWatcherTests()
        {
            this.property = new PropertyId(3);

            this.block1 = TestBlock.Regtest0;
            this.block2 = TestBlock.Regtest1;
            this.block3 = TestBlock.Regtest2;
            this.block4 = TestBlock.Regtest3;

            this.address1 = new ReceivingAddress(
                Guid.NewGuid(),
                TestAddress.Regtest1,
                true,
                new Collection<ReceivingAddressReservation>());

            this.address2 = new ReceivingAddress(
                Guid.NewGuid(),
                TestAddress.Regtest2,
                true,
                new Collection<ReceivingAddressReservation>());

            this.reservation1 = new ReceivingAddressReservation(Guid.NewGuid(), this.address1, DateTime.Now, null);
            this.reservation2 = new ReceivingAddressReservation(Guid.NewGuid(), this.address2, DateTime.Now, null);

            this.address1.Reservations.Add(this.reservation1);
            this.address2.Reservations.Add(this.reservation2);

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
                this.reservation1,
                new PropertyAmount(100),
                3,
                TimeSpan.FromHours(1),
                new TokenReceivingCallback(this.callback1, "timeout"));

            this.rule2 = new Rule(
                this.property,
                this.reservation2,
                new PropertyAmount(50),
                1,
                TimeSpan.FromMinutes(30),
                new TokenReceivingCallback(this.callback2, "watch-timeout"));

            this.logger = new Mock<ILogger<TokenReceivingWatcher>>();
            this.blocks = new Mock<IBlocksStorage>();
            this.addressPool = new Mock<IReceivingAddressPool>();
            this.exodusRetriever = new Mock<ITransactionRetriever>();
            this.rules = new Mock<IRuleRepository>();
            this.watches = new Mock<IWatchRepository>();
            this.callbacks = new Mock<ICallbackRepository>();
            this.callbackExecutor = new Mock<ICallbackExecuter>();
            this.timerScheduler = new FakeTimerScheduler();

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

            this.exodusRetriever
                .Setup(r => r.GetBalanceChangesAsync(this.block1.Transactions[0], It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    var amount = this.rule1.TargetAmount;

                    return new[]
                    {
                        new BalanceChange(TestAddress.Regtest3, -amount, this.property),
                        new BalanceChange(this.address1.Address, amount, this.property),
                    };
                });

            this.exodusRetriever
                .Setup(r => r.GetBalanceChangesAsync(this.block2.Transactions[0], It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    var amount = this.rule2.TargetAmount / 2;

                    return new[]
                    {
                        new BalanceChange(this.address1.Address, -amount, this.property),
                        new BalanceChange(this.address2.Address, amount, this.property),
                    };
                });

            this.exodusRetriever
                .Setup(r => r.GetBalanceChangesAsync(this.block3.Transactions[0], It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    var amount = this.rule2.TargetAmount / 2;

                    return new[]
                    {
                        new BalanceChange(TestAddress.Regtest3, -amount, this.property),
                        new BalanceChange(this.address2.Address, amount, this.property),
                    };
                });

            this.callbacks
                .Setup(r => r.GetAsync(this.callback1.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.callback1);

            this.callbacks
                .Setup(r => r.GetAsync(this.callback2.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.callback2);

            this.subject = new TokenReceivingWatcher(
                this.property,
                this.logger.Object,
                this.blocks.Object,
                this.addressPool.Object,
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
                () => new TokenReceivingWatcher(
                    null,
                    this.logger.Object,
                    this.blocks.Object,
                    this.addressPool.Object,
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
                () => new TokenReceivingWatcher(
                    this.property,
                    null,
                    this.blocks.Object,
                    this.addressPool.Object,
                    this.exodusRetriever.Object,
                    this.rules.Object,
                    this.watches.Object,
                    this.callbacks.Object,
                    this.callbackExecutor.Object,
                    this.timerScheduler));
        }

        [Fact]
        public void Constructor_WithNullAddressPool_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "addressPool",
                () => new TokenReceivingWatcher(
                    this.property,
                    this.logger.Object,
                    this.blocks.Object,
                    null,
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
                () => new TokenReceivingWatcher(
                    this.property,
                    this.logger.Object,
                    this.blocks.Object,
                    this.addressPool.Object,
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
                () => new TokenReceivingWatcher(
                    this.property,
                    this.logger.Object,
                    this.blocks.Object,
                    this.addressPool.Object,
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
                () => new TokenReceivingWatcher(
                    this.property,
                    this.logger.Object,
                    this.blocks.Object,
                    this.addressPool.Object,
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
                () => new TokenReceivingWatcher(
                    this.property,
                    this.logger.Object,
                    this.blocks.Object,
                    this.addressPool.Object,
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
                () => new TokenReceivingWatcher(
                    this.property,
                    this.logger.Object,
                    this.blocks.Object,
                    this.addressPool.Object,
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
                () => new TokenReceivingWatcher(
                    this.property,
                    this.logger.Object,
                    this.blocks.Object,
                    this.addressPool.Object,
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
                Assert.Equal(this.address1.Address, schedule.Context);

                this.rules.Verify(
                    r => r.ListUncompletedAsync(this.property, cancellationToken),
                    Times.Once());

                this.rules.Verify(
                    r => r.GetCurrentTimeoutAsync(this.rule1.Id, CancellationToken.None),
                    Times.Once());
            });
        }

        [Fact]
        public Task StartWatchAsync_WithNullAddress_ShouldThrow()
        {
            return Assert.ThrowsAsync<ArgumentNullException>(
                "address",
                () => this.subject.StartWatchAsync(
                    null,
                    this.rule1.TargetAmount,
                    this.rule1.TargetConfirmation,
                    this.rule1.OriginalTimeout,
                    this.rule1.Callback,
                    CancellationToken.None));
        }

        [Fact]
        public Task StartWatchAsync_WithReleasedAddress_ShouldThrow()
        {
            var reservation = new ReceivingAddressReservation(
                Guid.NewGuid(),
                this.address1,
                DateTime.Now,
                DateTime.Now);

            return Assert.ThrowsAsync<ArgumentException>(
                "address",
                () => this.subject.StartWatchAsync(
                    reservation,
                    this.rule1.TargetAmount,
                    this.rule1.TargetConfirmation,
                    this.rule1.OriginalTimeout,
                    this.rule1.Callback,
                    CancellationToken.None));
        }

        [Fact]
        public Task StartWatchAsync_WithUnsupportedTimeout_ShouldThrow()
        {
            this.timerScheduler.DurationValidator = duration => duration == this.rule1.OriginalTimeout;

            return Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                "timeout",
                () => this.subject.StartWatchAsync(
                    this.rule1.AddressReservation,
                    this.rule1.TargetAmount,
                    this.rule1.TargetConfirmation,
                    TimeSpan.Zero,
                    this.rule1.Callback,
                    CancellationToken.None));
        }

        [Fact]
        public Task StartWatchAsync_WithCompletedCallback_ShouldThrow()
        {
            var callback = new TokenReceivingCallback(
                new Callback(
                    Guid.NewGuid(),
                    IPAddress.Parse("192.168.1.2"),
                    DateTime.Now,
                    true,
                    new Uri("http://localhost")),
                "timeout");

            return Assert.ThrowsAsync<ArgumentException>(
                "callback",
                () => this.subject.StartWatchAsync(
                    this.rule1.AddressReservation,
                    this.rule1.TargetAmount,
                    this.rule1.TargetConfirmation,
                    this.rule1.OriginalTimeout,
                    callback,
                    CancellationToken.None));
        }

        [Fact]
        public Task StartWatchAsync_NotStarted_ShouldThrow()
        {
            return Assert.ThrowsAsync<InvalidOperationException>(
                () => this.subject.StartWatchAsync(
                    this.rule1.AddressReservation,
                    this.rule1.TargetAmount,
                    this.rule1.TargetConfirmation,
                    this.rule1.OriginalTimeout,
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
                    this.rule1.AddressReservation,
                    this.rule1.TargetAmount,
                    this.rule1.TargetConfirmation,
                    this.rule1.OriginalTimeout,
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
                var rule = await InvokeStartWatchAsync(this.rule1, cancellationToken);

                // Assert.
                this.rules.Verify(
                    r => r.AddAsync(CreateRuleMatcher(rule), cancellationToken),
                    Times.Once());

                var schedule = Assert.Single(this.timerScheduler.ActiveSchedules);

                Assert.Equal(rule.AddressReservation.Address.Address, schedule.Context);
                Assert.Equal(rule.OriginalTimeout, schedule.Due);
                Assert.Null(schedule.Period);

                await this.subject.StopAsync(CancellationToken.None);

                Assert.Single(this.timerScheduler.StoppedSchedules, schedule);

                this.rules.Verify(
                    r => r.DecreaseTimeoutAsync(
                        rule.Id,
                        It.Is<TimeSpan>(v => v > TimeSpan.Zero),
                        It.IsAny<CancellationToken>()),
                    Times.Once());
            });
        }

        [Fact]
        public async Task StartWatchAsync_WatchingHaveBeenTimeoutWithCallback_ShouldRaiseCallback()
        {
            // Arrange.
            await StartSubjectAsync();

            var rule = await InvokeStartWatchAsync(this.rule2, CancellationToken.None);

            this.watches
                .Setup(r => r.TransitionToTimedOutAsync(rule, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Dictionary<Watch, int>()
                {
                    {
                        new Watch(
                            rule,
                            this.block2.GetHash(),
                            this.block2.Transactions[0].GetHash(),
                            this.address2.Address,
                            rule.TargetAmount / 2),
                        1
                    },
                    {
                        new Watch(
                            rule,
                            this.block3.GetHash(),
                            this.block3.Transactions[0].GetHash(),
                            this.address2.Address,
                            rule.TargetAmount / 2),
                        0
                    }
                });

            // Act.
            this.timerScheduler.Trigger(s => s.Context.Equals(this.address2.Address));

            await this.subject.StopAsync(CancellationToken.None);

            // Assert.
            var expect = new CallbackResult(
                rule.Callback.TimeoutStatus,
                new CallbackData()
                {
                    Received = new CallbackAmount()
                    {
                        Confirmed = rule.TargetAmount / 2,
                        Pending = null,
                    },
                });

            this.rules.Verify(
                r => r.SetTimedOutAsync(rule.Id, CancellationToken.None),
                Times.Once());

            this.addressPool.Verify(
                p => p.ReleaseAddressAsync(this.reservation2.Id, CancellationToken.None),
                Times.Once());

            this.watches.Verify(
                r => r.TransitionToTimedOutAsync(CreateRuleMatcher(rule), CancellationToken.None),
                Times.Once());

            this.callbacks.Verify(
                r => r.AddHistoryAsync(this.callback2.Id, expect, CancellationToken.None),
                Times.Once());

            this.callbackExecutor.Verify(
                e => e.ExecuteAsync(this.callback2.Id, this.callback2.Url, expect, CancellationToken.None),
                Times.Once());

            this.callbacks.Verify(
                r => r.SetCompletedAsyc(this.callback2.Id, CancellationToken.None),
                Times.Once());

            this.rules.Verify(
                r => r.DecreaseTimeoutAsync(rule.Id, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
                Times.Never());
        }

        [Fact]
        public async Task StartWatchAsync_WatchingHaveBeenTimeoutWithNoCallback_ShouldNotRaiseCallback()
        {
            // Arrange.
            await StartSubjectAsync();

            var id = await this.subject.StartWatchAsync(
                this.rule2.AddressReservation,
                this.rule2.TargetAmount,
                this.rule2.TargetConfirmation,
                this.rule2.OriginalTimeout,
                null,
                CancellationToken.None);

            var rule = new Rule(
                this.rule2.Property,
                this.rule2.AddressReservation,
                this.rule2.TargetAmount,
                this.rule2.TargetConfirmation,
                this.rule2.OriginalTimeout,
                null,
                id);

            this.watches
                .Setup(r => r.TransitionToTimedOutAsync(rule, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Dictionary<Watch, int>()
                {
                    {
                        new Watch(
                            rule,
                            this.block2.GetHash(),
                            this.block2.Transactions[0].GetHash(),
                            this.address2.Address,
                            rule.TargetAmount / 2),
                        1
                    },
                    {
                        new Watch(
                            rule,
                            this.block3.GetHash(),
                            this.block3.Transactions[0].GetHash(),
                            this.address2.Address,
                            rule.TargetAmount / 2),
                        0
                    }
                });

            // Act.
            this.timerScheduler.Trigger(s => s.Context.Equals(this.address2.Address));

            await this.subject.StopAsync(CancellationToken.None);

            // Assert.
            this.rules.Verify(
                r => r.SetTimedOutAsync(rule.Id, CancellationToken.None),
                Times.Once());

            this.addressPool.Verify(
                p => p.ReleaseAddressAsync(this.reservation2.Id, CancellationToken.None),
                Times.Once());

            this.watches.Verify(
                r => r.TransitionToTimedOutAsync(CreateRuleMatcher(rule), CancellationToken.None),
                Times.Once());

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
                var address = rule.AddressReservation.Address.Address;

                var confirm = new Confirmation(
                    this.block3.GetHash(),
                    address,
                    new Dictionary<Watch, int>()
                    {
                        {
                            new Watch(
                                rule,
                                this.block1.GetHash(),
                                this.block1.Transactions[0].GetHash(),
                                address,
                                rule.TargetAmount),
                            3
                        }
                    });

                await InvokeConfirmationUpdateAsync(
                    confirm,
                    3,
                    ConfirmationType.Confirmed,
                    cancellationToken);

                // Act.
                this.timerScheduler.Trigger(s => s.Context.Equals(this.address1.Address));

                // Assert.
                this.rules.Verify(
                    r => r.SetSucceededAsync(rule.Id, CancellationToken.None),
                    Times.Once());

                this.rules.Verify(
                    r => r.SetTimedOutAsync(rule.Id, It.IsAny<CancellationToken>()),
                    Times.Never());

                this.addressPool.Verify(
                    p => p.ReleaseAddressAsync(rule.AddressReservation.Id, It.IsAny<CancellationToken>()),
                    Times.Once());

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
            var address = rule.AddressReservation.Address.Address;

            this.rules
                .Setup(r => r.SetTimedOutAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception());

            // Act.
            this.timerScheduler.Trigger(s => s.Context.Equals(address));

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

                this.timerScheduler.Trigger(s => s.Context.Equals(this.address1.Address));

                // Act.
                await this.subject.StopAsync(cancellationToken);

                // Assert.
                this.rules.Verify(
                    r => r.DecreaseTimeoutAsync(this.rule1.Id, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
                    Times.Never());

                this.rules.Verify(
                    r => r.DecreaseTimeoutAsync(
                        this.rule2.Id,
                        It.Is<TimeSpan>(v => v > TimeSpan.Zero),
                        CancellationToken.None),
                    Times.Once());

                Assert.Single(this.timerScheduler.StoppedSchedules, s => s.Context.Equals(this.address2.Address));
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
                    this.block1.GetHash(),
                    this.block1.Transactions[0].GetHash(),
                    this.address1.Address,
                    this.rule1.TargetAmount);
                var watch2 = new Watch(
                    this.rule2,
                    this.block2.GetHash(),
                    this.block2.Transactions[0].GetHash(),
                    this.address2.Address,
                    this.rule2.TargetAmount / 2);

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

            this.watches
                .Setup(r => r.TransitionToTimedOutAsync(this.rule1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Dictionary<Watch, int>());

            this.timerScheduler.Trigger(i => i.Context.Equals(this.address1.Address));

            var confirm = new Confirmation(
                this.block3.GetHash(),
                this.address1.Address,
                new Dictionary<Watch, int>()
                {
                    {
                        new Watch(
                            this.rule1,
                            this.block1.GetHash(),
                            this.block1.Transactions[0].GetHash(),
                            this.address1.Address,
                            this.rule1.TargetAmount),
                        3
                    }
                });

            // Act.
            var result = await InvokeConfirmationUpdateAsync(
                confirm,
                3,
                ConfirmationType.Confirmed,
                CancellationToken.None);

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

            this.addressPool.Verify(
                r => r.ReleaseAddressAsync(this.reservation1.Id, It.IsAny<CancellationToken>()),
                Times.Once());

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
        public Task ConfirmationUpdateAsync_WithBlockUnconfirming_ShouldUpdateConfirmationCountOnly()
        {
            return AsynchronousTesting.WithCancellationTokenAsync(async cancellationToken =>
            {
                // Arrange.
                await StartSubjectAsync(this.rule1);

                var confirm = new Confirmation(
                    this.block1.GetHash(),
                    this.address1.Address,
                    new Dictionary<Watch, int>()
                    {
                        {
                            new Watch(
                                this.rule1,
                                this.block1.GetHash(),
                                this.block1.Transactions[0].GetHash(),
                                this.address1.Address,
                                this.rule1.TargetAmount),
                            1
                        }
                    });

                // Act.
                var result = await InvokeConfirmationUpdateAsync(
                    confirm,
                    1,
                    ConfirmationType.Unconfirming,
                    cancellationToken);

                // Assert.
                var expect = confirm.Watches.ToDictionary(p => p.Key, p => p.Value - 1);

                Assert.False(result);

                this.watches.Verify(r => r.SetConfirmationCountAsync(expect, cancellationToken), Times.Once());

                this.rules.Verify(
                    r => r.SetSucceededAsync(this.rule1.Id, It.IsAny<CancellationToken>()),
                    Times.Never());

                this.addressPool.Verify(
                    p => p.ReleaseAddressAsync(this.rule1.AddressReservation.Id, It.IsAny<CancellationToken>()),
                    Times.Never());

                this.callbacks.Verify(
                    r => r.AddHistoryAsync(
                        this.callback1.Id,
                        It.IsAny<CallbackResult>(),
                        It.IsAny<CancellationToken>()),
                    Times.Never());

                this.callbackExecutor.Verify(
                    e => e.ExecuteAsync(
                        this.callback1.Id,
                        It.IsAny<Uri>(),
                        It.IsAny<CallbackResult>(),
                        It.IsAny<CancellationToken>()),
                    Times.Never());

                this.callbacks.Verify(
                    r => r.SetCompletedAsyc(this.callback1.Id, It.IsAny<CancellationToken>()),
                    Times.Never());

                Assert.Empty(this.timerScheduler.StoppedSchedules);

                await this.subject.StopAsync(CancellationToken.None);

                this.rules.Verify(
                    r => r.DecreaseTimeoutAsync(this.rule1.Id, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
                    Times.Once());
            });
        }

        [Fact]
        public Task ConfirmationUpdateAsync_NoConfirmedAmount_ShouldNotComplete()
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
                            this.address1.Address,
                            this.rule1.TargetAmount),
                        2
                    }
                };

                var confirm = new Confirmation(this.block2.GetHash(), this.address1.Address, watches);

                // Act.
                var result = await InvokeConfirmationUpdateAsync(
                    confirm,
                    2,
                    ConfirmationType.Confirmed,
                    cancellationToken);

                // Assert.
                Assert.False(result);

                this.watches.Verify(
                    r => r.SetConfirmationCountAsync(watches, cancellationToken),
                    Times.Once());

                this.rules.Verify(
                    r => r.SetSucceededAsync(this.rule1.Id, It.IsAny<CancellationToken>()),
                    Times.Never());

                this.addressPool.Verify(
                    p => p.ReleaseAddressAsync(this.reservation1.Id, It.IsAny<CancellationToken>()),
                    Times.Never());

                this.callbacks.Verify(
                    r => r.AddHistoryAsync(
                        this.callback1.Id,
                        It.IsAny<CallbackResult>(),
                        It.IsAny<CancellationToken>()),
                    Times.Never());

                this.callbackExecutor.Verify(
                    e => e.ExecuteAsync(
                        this.callback1.Id,
                        It.IsAny<Uri>(),
                        It.IsAny<CallbackResult>(),
                        It.IsAny<CancellationToken>()),
                    Times.Never());

                this.callbacks.Verify(
                    r => r.SetCompletedAsyc(this.callback1.Id, It.IsAny<CancellationToken>()),
                    Times.Never());

                Assert.Empty(this.timerScheduler.StoppedSchedules);

                await this.subject.StopAsync(CancellationToken.None);

                this.rules.Verify(
                    r => r.DecreaseTimeoutAsync(
                        this.rule1.Id,
                        It.IsAny<TimeSpan>(),
                        It.IsAny<CancellationToken>()),
                    Times.Once());
            });
        }

        [Fact]
        public Task ConfirmationUpdateAsync_ConfirmedAmountIsNotEnough_ShouldNotComplete()
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
                            this.address2.Address,
                            this.rule2.TargetAmount / 2),
                        1
                    }
                };

                var confirm = new Confirmation(this.block2.GetHash(), this.address2.Address, watches);

                // Act.
                var result = await InvokeConfirmationUpdateAsync(
                    confirm,
                    1,
                    ConfirmationType.Confirmed,
                    cancellationToken);

                // Assert.
                Assert.False(result);

                this.watches.Verify(
                    r => r.SetConfirmationCountAsync(watches, cancellationToken),
                    Times.Once());

                this.rules.Verify(
                    r => r.SetSucceededAsync(this.rule2.Id, It.IsAny<CancellationToken>()),
                    Times.Never());

                this.addressPool.Verify(
                    p => p.ReleaseAddressAsync(this.reservation2.Id, It.IsAny<CancellationToken>()),
                    Times.Never());

                this.callbacks.Verify(
                    r => r.AddHistoryAsync(
                        this.callback2.Id,
                        It.IsAny<CallbackResult>(),
                        It.IsAny<CancellationToken>()),
                    Times.Never());

                this.callbackExecutor.Verify(
                    e => e.ExecuteAsync(
                        this.callback2.Id,
                        It.IsAny<Uri>(),
                        It.IsAny<CallbackResult>(),
                        It.IsAny<CancellationToken>()),
                    Times.Never());

                this.callbacks.Verify(
                    r => r.SetCompletedAsyc(this.callback2.Id, It.IsAny<CancellationToken>()),
                    Times.Never());

                Assert.Empty(this.timerScheduler.StoppedSchedules);

                await this.subject.StopAsync(CancellationToken.None);

                this.rules.Verify(
                    r => r.DecreaseTimeoutAsync(
                        this.rule2.Id,
                        It.IsAny<TimeSpan>(),
                        It.IsAny<CancellationToken>()),
                    Times.Once());
            });
        }

        [Fact]
        public Task ConfirmationUpdateAsync_ConfirmedAmountIsEnoughWithCallback_ShouldRaiseCallback()
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
                            this.address2.Address,
                            this.rule2.TargetAmount / 2),
                        2
                    },
                    {
                        new Watch(
                            this.rule2,
                            this.block3.GetHash(),
                            this.block3.Transactions[0].GetHash(),
                            this.address2.Address,
                            this.rule2.TargetAmount / 2),
                        1
                    }
                };

                var confirm = new Confirmation(this.block3.GetHash(), this.address2.Address, watches);

                // Act.
                var result = await InvokeConfirmationUpdateAsync(
                    confirm,
                    1,
                    ConfirmationType.Confirmed,
                    cancellationToken);

                // Assert.
                var expect = new CallbackResult(
                    CallbackResult.StatusSuccess,
                    new CallbackData()
                    {
                        Received = new CallbackAmount()
                        {
                            Confirmed = this.rule2.TargetAmount,
                            Pending = null,
                        },
                    });

                Assert.True(result);

                this.watches.Verify(
                    r => r.SetConfirmationCountAsync(watches, cancellationToken),
                    Times.Once());

                this.rules.Verify(
                    r => r.SetSucceededAsync(this.rule2.Id, CancellationToken.None),
                    Times.Once());

                this.addressPool.Verify(
                    p => p.ReleaseAddressAsync(this.reservation2.Id, It.IsAny<CancellationToken>()),
                    Times.Once());

                this.callbacks.Verify(
                    r => r.AddHistoryAsync(this.callback2.Id, expect, CancellationToken.None),
                    Times.Once());

                this.callbackExecutor.Verify(
                    e => e.ExecuteAsync(this.callback2.Id, this.callback2.Url, expect, CancellationToken.None),
                    Times.Once());

                this.callbacks.Verify(
                    r => r.SetCompletedAsyc(this.callback2.Id, CancellationToken.None),
                    Times.Once());

                var stopped = Assert.Single(this.timerScheduler.StoppedSchedules);

                Assert.Equal(this.address2.Address, stopped.Context);
                Assert.Equal(this.rule2.OriginalTimeout, stopped.Due);

                await this.subject.StopAsync(CancellationToken.None);

                this.rules.Verify(
                    r => r.DecreaseTimeoutAsync(this.rule2.Id, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
                    Times.Never());
            });
        }

        [Fact]
        public Task ConfirmationUpdateAsync_ConfirmedAmountIsEnoughWithNoCallback_ShouldNotRaiseCallback()
        {
            return AsynchronousTesting.WithCancellationTokenAsync(async cancellationToken =>
            {
                // Arrange.
                var rule = new Rule(
                    this.rule2.Property,
                    this.rule2.AddressReservation,
                    this.rule2.TargetAmount,
                    this.rule2.TargetConfirmation,
                    this.rule2.OriginalTimeout,
                    null);

                await StartSubjectAsync(rule);

                var watches = new Dictionary<Watch, int>()
                {
                    {
                        new Watch(
                            rule,
                            this.block2.GetHash(),
                            this.block2.Transactions[0].GetHash(),
                            this.address2.Address,
                            rule.TargetAmount / 2),
                        2
                    },
                    {
                        new Watch(
                            rule,
                            this.block3.GetHash(),
                            this.block3.Transactions[0].GetHash(),
                            this.address2.Address,
                            rule.TargetAmount / 2),
                        1
                    }
                };

                var confirm = new Confirmation(this.block3.GetHash(), this.address2.Address, watches);

                // Act.
                var result = await InvokeConfirmationUpdateAsync(
                    confirm,
                    1,
                    ConfirmationType.Confirmed,
                    cancellationToken);

                // Assert.
                Assert.True(result);

                this.watches.Verify(
                    r => r.SetConfirmationCountAsync(watches, cancellationToken),
                    Times.Once());

                this.rules.Verify(
                    r => r.SetSucceededAsync(rule.Id, CancellationToken.None),
                    Times.Once());

                this.addressPool.Verify(
                    p => p.ReleaseAddressAsync(this.reservation2.Id, It.IsAny<CancellationToken>()),
                    Times.Once());

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

                var stopped = Assert.Single(this.timerScheduler.StoppedSchedules);

                Assert.Equal(this.address2.Address, stopped.Context);
                Assert.Equal(rule.OriginalTimeout, stopped.Due);

                await this.subject.StopAsync(CancellationToken.None);

                this.rules.Verify(
                    r => r.DecreaseTimeoutAsync(rule.Id, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
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
                        new BalanceChange(TestAddress.Regtest3, -this.rule1.TargetAmount, property),
                        new BalanceChange(this.address1.Address, this.rule1.TargetAmount, property),
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

                await StartSubjectAsync(this.rule1, this.rule2);

                // Act.
                var result = await InvokeGetBalanceChangesAsync(tx, cancellationToken);

                // Assert.
                var change = Assert.Single(result);

                Assert.Equal(this.address2.Address, change.Key);
                Assert.Equal(this.rule2.TargetAmount / 2, change.Value.Amount);
                Assert.Equal(this.rule2, change.Value.Context);

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
                        this.address1.Address,
                        this.rule1.TargetAmount)
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
                        this.address1.Address,
                        this.rule1.TargetAmount)
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

        Rule CreateRuleMatcher(Guid id, Rule match)
        {
            Expression<Func<Rule, bool>> compare = r =>
                r.AddressReservation.Id == match.AddressReservation.Id &&
                r.Callback == match.Callback &&
                r.Id == id &&
                r.OriginalTimeout == match.OriginalTimeout &&
                r.Property == match.Property &&
                r.TargetAmount == match.TargetAmount &&
                r.TargetConfirmation == match.TargetConfirmation;

            return It.Is<Rule>(compare);
        }

        Rule CreateRuleMatcher(Rule match)
        {
            return CreateRuleMatcher(match.Id, match);
        }

        Rule Clone(Guid id, Rule source)
        {
            return new Rule(
                source.Property,
                source.AddressReservation,
                source.TargetAmount,
                source.TargetConfirmation,
                source.OriginalTimeout,
                source.Callback,
                id);
        }

        async Task<Rule> InvokeStartWatchAsync(Rule rule, CancellationToken cancellationToken)
        {
            var id = await this.subject.StartWatchAsync(
                rule.AddressReservation,
                rule.TargetAmount,
                rule.TargetConfirmation,
                rule.OriginalTimeout,
                rule.Callback,
                cancellationToken);

            return Clone(id, rule);
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
            return ((IBalanceWatcherHandler<Rule, PropertyAmount>)this.subject).GetBalanceChangesAsync(
                tx,
                cancellationToken);
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
