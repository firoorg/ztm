using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using Xunit;
using Ztm.Data.Entity.Testing;

namespace Ztm.WebApi.Tests
{
    public class SqlTransactionConfirmationWatchingRuleRepositoryTests : IDisposable
    {
        readonly SqlTransactionConfirmationWatchingRuleRepository<TestCallbackResult> subject;
        readonly SqlCallbackRepository callbackRepository;
        readonly TestMainDatabaseFactory dbFactory;

        private Callback defaultCallback;

        public SqlTransactionConfirmationWatchingRuleRepositoryTests()
        {
            this.dbFactory = new TestMainDatabaseFactory();
            this.subject = new SqlTransactionConfirmationWatchingRuleRepository<TestCallbackResult>(dbFactory);
            this.callbackRepository = new SqlCallbackRepository(dbFactory);
        }

        public void Dispose()
        {
            this.dbFactory.Dispose();
        }

        [Fact]
        public void Construct_WithNullDatabaseFactory_ShouldFail()
        {
            Assert.Throws<ArgumentNullException>(
                "db",
                () => new SqlTransactionConfirmationWatchingRuleRepository<TestCallbackResult>(null)
            );
        }

        [Fact]
        public async Task AddAsync_WithValidArgs_ShouldSuccess()
        {
            // Arrange.
            await this.CreateDefaultCallback();

            var transaction = uint256.Parse("008b3395991c7893bb8a82d8389a48ded863af914d9cc31711554bc97e4723c0");
            var successResult = new TestCallbackResult(CallbackResult.StatusSuccess, "success");
            var timeoutResult = new TestCallbackResult(CallbackResult.StatusError, "timeout");
            var waitingTime = TimeSpan.FromMinutes(5);

            // Act.
            var watch = await this.subject.AddAsync(transaction, 10, waitingTime,
                successResult, timeoutResult, this.defaultCallback, CancellationToken.None);

            // Assert.
            Assert.NotEqual(Guid.Empty, watch.Id);
            Assert.Equal(transaction, watch.Transaction);
            Assert.Equal(10, watch.Confirmation);
            Assert.Equal(waitingTime, watch.WaitingTime);
            Assert.Equal(waitingTime, await this.subject.GetRemainingWaitingTimeAsync(watch.Id, CancellationToken.None));
            Assert.Equal(successResult, watch.Success);
            Assert.Equal(timeoutResult, watch.Timeout);
        }

        [Fact]
        public async Task AddAsync_WithInvalidArgs_ShouldThrow()
        {
            var transaction = uint256.Parse("008b3395991c7893bb8a82d8389a48ded863af914d9cc31711554bc97e4723c0");
            var successResult = new TestCallbackResult(CallbackResult.StatusSuccess, "success");
            var timeoutResult = new TestCallbackResult(CallbackResult.StatusError, "timeout");
            var timeout = TimeSpan.FromMinutes(5);

            await Assert.ThrowsAsync<ArgumentNullException>(
                "transaction",
                () => this.subject.AddAsync(null, 0, timeout, successResult, timeoutResult, this.defaultCallback, CancellationToken.None)
            );

            await Assert.ThrowsAsync<ArgumentNullException>(
                "successData",
                () => this.subject.AddAsync(transaction, 0, timeout, null, timeoutResult, this.defaultCallback, CancellationToken.None)
            );

            await Assert.ThrowsAsync<ArgumentNullException>(
                "timeoutData",
                () => this.subject.AddAsync(transaction, 0, timeout, successResult, null, this.defaultCallback, CancellationToken.None)
            );

            await Assert.ThrowsAsync<ArgumentNullException>(
                "callback",
                () => this.subject.AddAsync(transaction, 0, timeout, successResult, timeoutResult, null, CancellationToken.None)
            );
        }

        [Fact]
        public async Task GetAsync_ExistWatch_ShouldSuccess()
        {
            // Arrange.
            await this.CreateDefaultCallback();

            var transaction = uint256.Parse("008b3395991c7893bb8a82d8389a48ded863af914d9cc31711554bc97e4723c0");
            var successResult = new TestCallbackResult(CallbackResult.StatusSuccess, "success");
            var timeoutResult = new TestCallbackResult(CallbackResult.StatusError, "timeout");
            var timeout = TimeSpan.FromMinutes(5);

            var watch = await this.subject.AddAsync(transaction, 10, timeout,
                successResult, timeoutResult, this.defaultCallback, CancellationToken.None);

            // Act.
            var retrieved = await this.subject.GetAsync(watch.Id, CancellationToken.None);

            // Assert.
            Assert.Equal(watch.Id, retrieved.Id);
            Assert.Equal(watch.Transaction, retrieved.Transaction);
            Assert.Equal(watch.Confirmation, retrieved.Confirmation);
            Assert.Equal(watch.WaitingTime, retrieved.WaitingTime);
            Assert.Equal(watch.Success, retrieved.Success);
            Assert.Equal(watch.Timeout, retrieved.Timeout);
            Assert.Equal(this.defaultCallback, retrieved.Callback);
        }

        [Fact]
        public async Task GetAsync_NonExistWatch_ShouldReturnNull()
        {
            Assert.Null(await this.subject.GetAsync(Guid.NewGuid(), CancellationToken.None));
        }

        [Fact]
        public async Task ListActiveAsync_WithNonEmptyList_ShouldSuccess()
        {
            // Arrange.
            await this.CreateDefaultCallback();

            var transaction = uint256.Parse("008b3395991c7893bb8a82d8389a48ded863af914d9cc31711554bc97e4723c0");
            var successResult = new TestCallbackResult(CallbackResult.StatusSuccess, "success");
            var timeoutResult = new TestCallbackResult(CallbackResult.StatusError, "timeout");

            var watches = new List<TransactionConfirmationWatchingRule<TestCallbackResult>>();
            watches.Add(await this.subject.AddAsync(transaction, 10, TimeSpan.FromMinutes(5),
                successResult, timeoutResult, this.defaultCallback, CancellationToken.None));

            watches.Add(await this.subject.AddAsync(transaction, 11, TimeSpan.FromHours(6),
                successResult, timeoutResult, this.defaultCallback, CancellationToken.None));

            watches = watches.Select(async watch => await this.subject.GetAsync(watch.Id, CancellationToken.None))
                .Select(watche => watche.Result)
                .ToList();

            // Act.
            var retrieved = (await this.subject.ListActiveAsync(CancellationToken.None)).ToList();

            // Assert.
            Assert.Equal(2, retrieved.Count());
            Assert.Equal(watches[0].Id, retrieved[0].Id);
            Assert.Equal(watches[0].Transaction, retrieved[0].Transaction);
            Assert.Equal(watches[0].Confirmation, retrieved[0].Confirmation);
            Assert.Equal(watches[0].WaitingTime, retrieved[0].WaitingTime);
            Assert.Equal(watches[0].Success, retrieved[0].Success);
            Assert.Equal(watches[0].Timeout, retrieved[0].Timeout);
            Assert.Equal(watches[0].Callback, retrieved[0].Callback);

            Assert.Equal(watches[1].Id, retrieved[1].Id);
            Assert.Equal(watches[1].Callback, retrieved[1].Callback);
        }

        [Fact]
        public async Task ListActiveAsync_WithEmptyList_ShouldReturnEmptyIEnumerable()
        {
            Assert.Empty(await this.subject.ListActiveAsync(CancellationToken.None));
        }

        [Fact]
        public async Task ListActiveAsync_NonPendingRule_ShouldBeFilterOut()
        {
            // Arrange.
            await this.CreateDefaultCallback();

            var transaction = uint256.Parse("008b3395991c7893bb8a82d8389a48ded863af914d9cc31711554bc97e4723c0");
            var successResult = new TestCallbackResult(CallbackResult.StatusSuccess, "success");
            var timeoutResult = new TestCallbackResult(CallbackResult.StatusError, "timeout");

            var watches = new List<TransactionConfirmationWatchingRule<TestCallbackResult>>();
            watches.Add(await this.subject.AddAsync(transaction, 10, TimeSpan.FromMinutes(5),
                successResult, timeoutResult, this.defaultCallback, CancellationToken.None));

            await this.subject.UpdateStatusAsync(watches.Last().Id, TransactionConfirmationWatchingRuleStatus.Success, CancellationToken.None);

            // Act.
            var retrieved = (await this.subject.ListActiveAsync(CancellationToken.None)).ToList();

            // Assert.
            Assert.Empty(retrieved);
        }

        [Fact]
        public void SetRemainingWaitingTimeAsync_WithNotExist_ShouldThrow()
        {
            _ = Assert.ThrowsAsync<KeyNotFoundException>(
                () => this.subject.SubtractRemainingWaitingTimeAsync(Guid.NewGuid(), TimeSpan.FromDays(1), CancellationToken.None)
            );
        }

        [Fact]
        public async Task SubtractRemainingWaitingTimeAsync_WithValueLargerThanCurrent_ResultShouldBeZero()
        {
            // Arrange.
            await this.CreateDefaultCallback();

            var transaction = uint256.Parse("008b3395991c7893bb8a82d8389a48ded863af914d9cc31711554bc97e4723c0");
            var successResult = new TestCallbackResult(CallbackResult.StatusSuccess, "success");
            var timeoutResult = new TestCallbackResult(CallbackResult.StatusError, "timeout");
            var waitingTime = TimeSpan.FromMinutes(5);

            var watch = await this.subject.AddAsync(transaction, 10, waitingTime,
                successResult, timeoutResult, this.defaultCallback, CancellationToken.None);

            // Act.
            await this.subject.SubtractRemainingWaitingTimeAsync(watch.Id, TimeSpan.FromMinutes(6), CancellationToken.None);

            // Assert.
            Assert.Equal(TimeSpan.Zero, await this.subject.GetRemainingWaitingTimeAsync(watch.Id, CancellationToken.None));
        }

        [Fact]
        public async Task SubtractRemainingWaitingTimeAsync_WithValidTimeSpan_ShouldSuccess()
        {
            // Arrange.
            await this.CreateDefaultCallback();

            var transaction = uint256.Parse("008b3395991c7893bb8a82d8389a48ded863af914d9cc31711554bc97e4723c0");
            var successResult = new TestCallbackResult(CallbackResult.StatusSuccess, "success");
            var timeoutResult = new TestCallbackResult(CallbackResult.StatusError, "timeout");
            var waitingTime = TimeSpan.FromMinutes(5);

            var watch = await this.subject.AddAsync(transaction, 10, waitingTime,
                successResult, timeoutResult, this.defaultCallback, CancellationToken.None);

            // Act.
            await this.subject.SubtractRemainingWaitingTimeAsync(watch.Id, TimeSpan.FromMinutes(4), CancellationToken.None);

            // Assert.
            var updated = await this.subject.GetAsync(watch.Id, CancellationToken.None);
            Assert.Equal(TimeSpan.FromMinutes(1), await this.subject.GetRemainingWaitingTimeAsync(watch.Id, CancellationToken.None));
        }

        async Task CreateDefaultCallback()
        {
            this.defaultCallback = await this.callbackRepository.AddAsync
            (
                IPAddress.Loopback,
                new Uri("http://zcoin.io"),
                CancellationToken.None
            );
        }

        sealed class TestCallbackResult : CallbackResult
        {
            public TestCallbackResult(string status, object data)
            {
                this.Status = status;
                this.Data = data;
            }

            public override string Status { get; }
            public override object Data { get; }

            public override bool Equals(object other)
            {
                if (other == null || other.GetType() != GetType())
                {
                    return false;
                }

                var otherResult = (TestCallbackResult)other;
                return this.Status == otherResult.Status
                    && this.Data.ToString() == otherResult.Data.ToString();
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Status, Data);
            }
        }
    }
}