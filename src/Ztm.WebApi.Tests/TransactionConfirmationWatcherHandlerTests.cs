using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using NSubstitute;
using Xunit;
using Ztm.Zcoin.NBitcoin;
using Ztm.Zcoin.Synchronization.Watchers;

namespace Ztm.WebApi.Tests
{
    using ConfirmContext = TransactionConfirmationWatch<TransactionConfirmationCallbackResult>;
    public sealed class TransactionConfirmationWatcherHandlerTests : IDisposable
    {
        readonly TransactionConfirmationWatcherHandler subject;

        readonly ICallbackRepository callbackRepository;
        readonly ITransactionConfirmationWatchRepository<TransactionConfirmationCallbackResult> watchRepository;
        readonly ICallbackExecuter callbackExecuter;

        readonly Uri defaultUrl;
        readonly uint256 defaultTransaction;

        public TransactionConfirmationWatcherHandlerTests()
        {
            this.callbackRepository = Substitute.For<ICallbackRepository>();
            this.watchRepository = Substitute.For<ITransactionConfirmationWatchRepository<TransactionConfirmationCallbackResult>>();
            this.callbackExecuter = Substitute.For<ICallbackExecuter>();

            this.subject = new TransactionConfirmationWatcherHandler
            (
                this.callbackRepository,
                this.watchRepository,
                this.callbackExecuter
            );

            this.defaultUrl = new Uri("http://zcoin.io");
            this.defaultTransaction = uint256.Parse("7396ddaa275ed5492564277efc0844b4aeaa098020bc8d4b4dbc489134e49afd");
            MockCallbackRepository();
            MockWatchRepository();
        }

        public void Dispose()
        {
        }

        [Fact]
        public void Construct_WithInvalidArgs_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>
            (
                "callbackRepository",
                () => new TransactionConfirmationWatcherHandler(null, this.watchRepository, this.callbackExecuter)
            );

            Assert.Throws<ArgumentNullException>
            (
                "watchRepository",
                () => new TransactionConfirmationWatcherHandler(this.callbackRepository, null, this.callbackExecuter)
            );

            Assert.Throws<ArgumentNullException>
            (
                "callbackExecuter",
                () => new TransactionConfirmationWatcherHandler(this.callbackRepository, this.watchRepository, null)
            );
        }

        [Fact]
        public async void AddTransactionAsync_WithValidArgument_ShouldSuccess()
        {
            // Arrange.
            var builder = new WatchArgsBuilder();

            // Act.
            await builder.Call(this.subject.AddTransactionAsync);
            Thread.Sleep(TimeSpan.FromSeconds(3));

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
                ),
                Arg.Any<CancellationToken>()
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
        public async void AddTransactionAsync_AndWaitSomeWatchesToTimeout_ShouldCallExecute()
        {
            // Arrange.
            var builder = new WatchArgsBuilder();

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
                ),
                Arg.Any<CancellationToken>()
            );
        }

        [Fact]
        public async Task CreateContextsAsync_WithValidArgs_ShouldSuccess()
        {
            // Arrange.
            var tx = Transaction.Parse(TransactionData.Transaction1, ZcoinNetworks.Instance.Mainnet);
            var untrackedTx = Transaction.Parse(TransactionData.Transaction2, ZcoinNetworks.Instance.Mainnet);

            var builder = new WatchArgsBuilder();

            builder.timeout = TimeSpan.FromSeconds(1);
            builder.transaction = tx.GetHash();
            await builder.Call(this.subject.AddTransactionAsync);

            builder.timeout = TimeSpan.FromSeconds(2);
            await builder.Call(this.subject.AddTransactionAsync);

            // Act.
            var contexts = await this.subject.CreateContextsAsync(tx, CancellationToken.None);
            var untrackedTxContexts = await this.subject.CreateContextsAsync(untrackedTx, CancellationToken.None);

            // Assert.
            Assert.Equal(2, contexts.Count());
            Assert.Empty(untrackedTxContexts);
        }

        [Fact]
        public async void AddWatchesAsync_WithValidArgs_ShouldNotThrow()
        {
            // Arrange.
            var builder = new WatchArgsBuilder();
            var watch1 = await builder.Call(this.subject.AddTransactionAsync);
            var watch2 = await builder.Call(this.subject.AddTransactionAsync);

            var ids = new List<TransactionWatch<ConfirmContext>>()
            {
                new TransactionWatch<ConfirmContext>(watch1, uint256.Zero, uint256.Zero),
                new TransactionWatch<ConfirmContext>(watch2, uint256.Zero, uint256.Zero),
            };

            // Act.
            await this.subject.AddWatchesAsync(ids, CancellationToken.None);
        }

        [Fact]
        public async void AddWatchesAsync_WithNullWatches_ShouldThrow()
        {
            await Assert.ThrowsAsync<ArgumentNullException>
            (
                "watches",
                () => this.subject.AddWatchesAsync(null, CancellationToken.None)
            );
        }

        [Fact]
        public async void GetCurrentWatchesAsync_WithNonEmpty_ShouldReceivedWatches()
        {
            // Arrange.
            var builder = new WatchArgsBuilder();
            var watch1 = await builder.Call(this.subject.AddTransactionAsync);
            var watch2 = await builder.Call(this.subject.AddTransactionAsync);

            var watches = new List<TransactionWatch<ConfirmContext>>()
            {
                new TransactionWatch<ConfirmContext>(watch1, uint256.Zero, uint256.Zero),
                new TransactionWatch<ConfirmContext>(watch2, uint256.Zero, uint256.Zero),
            };

            await this.subject.AddWatchesAsync(watches, CancellationToken.None);

            // Act.
            var received = await this.subject.GetCurrentWatchesAsync(CancellationToken.None);

            // Assert.
            Assert.Equal(2, received.Count());
            Assert.Contains(received, w => w.Context == watch1);
            Assert.Contains(received, w => w.Context == watch2);
        }

        [Fact]
        public async void GetCurrentWatchesAsync_Empty_ShouldReceivedEmpty()
        {
            Assert.Empty(await this.subject.GetCurrentWatchesAsync(CancellationToken.None));
        }

        [Fact]
        // Timer must be stopped but watch object still is in the handler
        public async void ConfirmationUpdateAsync_WithValidWatch_ShouldSuccess()
        {
            // Arrange.
            var builder = new WatchArgsBuilder();

            builder.timeout = TimeSpan.FromSeconds(1);
            var watch = await builder.Call(this.subject.AddTransactionAsync);

            var watches = new List<TransactionWatch<ConfirmContext>>()
            {
                new TransactionWatch<ConfirmContext>(watch, uint256.Zero, builder.transaction),
            };

            await this.subject.AddWatchesAsync(watches, CancellationToken.None);

            // Act.
            var result = await this.subject.ConfirmationUpdateAsync(watches[0], 1, ConfirmationType.Confirmed, CancellationToken.None);
            Thread.Sleep(TimeSpan.FromSeconds(2));

            // Assert.
            Assert.False(result);

            _ = this.callbackExecuter.Received(0).Execute(
                Arg.Any<Guid>(),
                Arg.Any<Uri>(),
                Arg.Any<TransactionConfirmationCallbackResult>(),
                Arg.Any<CancellationToken>()
            );

            var received = await this.subject.GetCurrentWatchesAsync(CancellationToken.None);
            Assert.Equal(1, received.Count());
            Assert.Equal(watches[0].Id, received.First().Id);
        }

        [Fact]
        public async void ConfirmationUpdateAsync_AndReachRequiredConfirmations_ShouldCallSuccess()
        {
            // Arrange.
            var builder = new WatchArgsBuilder();

            builder.timeout = TimeSpan.FromSeconds(2);
            builder.confirmation = 10;
            var watch = await builder.Call(this.subject.AddTransactionAsync);

            var watches = new List<TransactionWatch<ConfirmContext>>()
            {
                new TransactionWatch<ConfirmContext>(watch, uint256.Zero, builder.transaction),
            };

            await this.subject.AddWatchesAsync(watches, CancellationToken.None);

            // Act.
            for (var confirmation = 1; confirmation <= builder.confirmation; confirmation++)
            {
                await this.subject.ConfirmationUpdateAsync(watches[0], confirmation, ConfirmationType.Confirmed, CancellationToken.None);
            }
            Thread.Sleep(TimeSpan.FromSeconds(2));

            // Assert.
            _ = this.callbackExecuter.Received(1).Execute(
                Arg.Any<Guid>(),
                Arg.Any<Uri>(),
                Arg.Is<TransactionConfirmationCallbackResult>
                (
                    result => result.Status == CallbackResult.StatusSuccess
                ),
                Arg.Any<CancellationToken>()
            );
        }

        [Fact]
        // Confirm before timeout and meet required after timeout
        public async void ConfirmationUpdateAsync_AndConfirmAfterTimeout_ShouldSuccess()
        {
            // Arrange.
            var builder = new WatchArgsBuilder();

            builder.timeout = TimeSpan.FromSeconds(1);
            builder.confirmation = 10;
            var watch = await builder.Call(this.subject.AddTransactionAsync);

            var watches = new List<TransactionWatch<ConfirmContext>>()
            {
                new TransactionWatch<ConfirmContext>(watch, uint256.Zero, builder.transaction),
            };

            await this.subject.AddWatchesAsync(watches, CancellationToken.None);

            // Act.
            for (var confirmation = 1; confirmation < builder.confirmation; confirmation++)
            {
                await this.subject.ConfirmationUpdateAsync(watches[0], confirmation, ConfirmationType.Confirmed, CancellationToken.None);
            }
            Thread.Sleep(TimeSpan.FromSeconds(2));
            await this.subject.ConfirmationUpdateAsync(watches[0], builder.confirmation, ConfirmationType.Confirmed, CancellationToken.None);

            // Assert.
            _ = this.callbackExecuter.Received(1).Execute(
                Arg.Any<Guid>(),
                Arg.Any<Uri>(),
                Arg.Is<TransactionConfirmationCallbackResult>
                (
                    result => result.Status == CallbackResult.StatusSuccess
                ),
                Arg.Any<CancellationToken>()
            );
        }

        [Fact]
        // Resume and Timeout
        public async void ConfirmationUpdateAsync_WithUnconfirm_TimerShouldbeResume()
        {
            // Arrange.
            var builder = new WatchArgsBuilder();

            builder.timeout = TimeSpan.FromSeconds(1);
            builder.confirmation = 10;
            var watch = await builder.Call(this.subject.AddTransactionAsync);

            var watches = new List<TransactionWatch<ConfirmContext>>()
            {
                new TransactionWatch<ConfirmContext>(watch, uint256.Zero, builder.transaction),
            };

            await this.subject.AddWatchesAsync(watches, CancellationToken.None);

            // Act.
            await this.subject.ConfirmationUpdateAsync(watches[0], 1, ConfirmationType.Confirmed, CancellationToken.None);
            await this.subject.ConfirmationUpdateAsync(watches[0], 2, ConfirmationType.Confirmed, CancellationToken.None);
            await this.subject.ConfirmationUpdateAsync(watches[0], 2, ConfirmationType.Unconfirming, CancellationToken.None);
            await this.subject.ConfirmationUpdateAsync(watches[0], 1, ConfirmationType.Unconfirming, CancellationToken.None);

            Thread.Sleep(TimeSpan.FromSeconds(2));

            // Assert.
            _ = this.callbackExecuter.Received(1).Execute(
                Arg.Any<Guid>(),
                Arg.Any<Uri>(),
                Arg.Is<TransactionConfirmationCallbackResult>
                (
                    result => result.Status == CallbackResult.StatusError
                ),
                Arg.Any<CancellationToken>()
            );
        }

        [Fact]
        public async void ConfirmationUpdateAsync_WithUnconfirmWhenAlreadyTimeout_CallbackShouldBeCalled()
        {
            // Arrange.
            var builder = new WatchArgsBuilder();

            builder.timeout = TimeSpan.FromSeconds(1);
            builder.confirmation = 10;
            var watch = await builder.Call(this.subject.AddTransactionAsync);

            var watches = new List<TransactionWatch<ConfirmContext>>()
            {
                new TransactionWatch<ConfirmContext>(watch, uint256.Zero, builder.transaction),
            };

            await this.subject.AddWatchesAsync(watches, CancellationToken.None);

            // Act.
            await this.subject.ConfirmationUpdateAsync(watches[0], 1, ConfirmationType.Confirmed, CancellationToken.None);
            Thread.Sleep(TimeSpan.FromSeconds(2));

            await this.subject.ConfirmationUpdateAsync(watches[0], 1, ConfirmationType.Unconfirming, CancellationToken.None);

            // Assert.
            _ = this.callbackExecuter.Received(1).Execute(
                Arg.Any<Guid>(),
                Arg.Any<Uri>(),
                Arg.Is<TransactionConfirmationCallbackResult>
                (
                    result => result.Status == CallbackResult.StatusError
                ),
                Arg.Any<CancellationToken>()
            );
        }

        [Fact]
        public async void RemoveWatchAsync_WithExistKey_ShouldSuccess()
        {
            // Arrange.
            var builder = new WatchArgsBuilder();

            builder.timeout = TimeSpan.FromSeconds(1);
            builder.confirmation = 10;
            var watch = await builder.Call(this.subject.AddTransactionAsync);

            var watches = new List<TransactionWatch<ConfirmContext>>()
            {
                new TransactionWatch<ConfirmContext>(watch, uint256.Zero, builder.transaction),
            };

            await this.subject.AddWatchesAsync(watches, CancellationToken.None);

            // Act.
            await this.subject.RemoveWatchAsync(watches[0], WatchRemoveReason.Completed, CancellationToken.None);

            // Assert.
            Assert.Empty(await this.subject.GetCurrentWatchesAsync(CancellationToken.None));
        }

        [Fact]
        public async void Initialize_WithNonEmptyRepository_ShouldInitializeWatches()
        {
            // Arrange.
            var tx = Transaction.Parse(TransactionData.Transaction1, ZcoinNetworks.Instance.Mainnet);

            var builder = new WatchArgsBuilder();
            builder.timeout = TimeSpan.FromSeconds(1);
            builder.transaction = tx.GetHash();

            var callback = await this.callbackRepository.AddAsync
            (
                builder.url, builder.callbackUrl, CancellationToken.None
            );

            var watch = await this.watchRepository.AddAsync
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

            var completedWatch = await this.watchRepository.AddAsync
            (
                builder.transaction, builder.confirmation, builder.timeout, builder.successData, builder.timeoutData, completedCallback, CancellationToken.None
            );

            // Act.
            var handler = new TransactionConfirmationWatcherHandler
            (
                this.callbackRepository,
                this.watchRepository,
                this.callbackExecuter
            );
            await handler.Initialize(CancellationToken.None);
            var retrievedCount = (await handler.CreateContextsAsync(tx, CancellationToken.None)).Count();
            Thread.Sleep(TimeSpan.FromSeconds(2));

            // Assert.
            Assert.Equal(1, retrievedCount);

            _ = this.callbackExecuter.Received(1).Execute(
                Arg.Any<Guid>(),
                Arg.Any<Uri>(),
                Arg.Is<TransactionConfirmationCallbackResult>
                (
                    result => result.Status == CallbackResult.StatusError
                ),
                Arg.Any<CancellationToken>()
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

            public WatchArgsBuilder()
            {
                this.transaction = uint256.Parse("7396ddaa275ed5492564277efc0844b4aeaa098020bc8d4b4dbc489134e49afd");
                this.confirmation = 10;
                this.timeout = TimeSpan.FromSeconds(1);
                this.url = IPAddress.Loopback;
                this.callbackUrl = new Uri("http://zcoin.io");
                this.successData = new TransactionConfirmationCallbackResult(CallbackResult.StatusSuccess, "success");
                this.timeoutData = new TransactionConfirmationCallbackResult(CallbackResult.StatusError, "timeout");
                this.cancellationToken = CancellationToken.None;
            }

            public T Call<T>(Func<uint256, int, TimeSpan, IPAddress, Uri,
                TransactionConfirmationCallbackResult,
                TransactionConfirmationCallbackResult,
                CancellationToken, T> func)
            {
                return func(transaction, confirmation, timeout, url, callbackUrl,
                    successData, timeoutData, cancellationToken);
            }
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

        Dictionary<Guid, TransactionConfirmationWatch<TransactionConfirmationCallbackResult>> mockedWatchs;

        void MockWatchRepository()
        {
            mockedWatchs = new Dictionary<Guid, TransactionConfirmationWatch<TransactionConfirmationCallbackResult>>();

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
                            info.ArgAt<int>(1),
                            DateTime.UtcNow.Add(info.ArgAt<TimeSpan>(2)),
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

        }
    }
}