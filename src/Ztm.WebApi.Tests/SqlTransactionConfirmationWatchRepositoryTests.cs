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
    public class SqlTransactionConfirmationWatchRepositoryTests : IDisposable
    {
        readonly SqlTransactionConfirmationWatchRepository<TestCallbackResult> subject;
        readonly SqlCallbackRepository callbackRepository;
        readonly TestMainDatabaseFactory dbFactory;

        private Callback defaultCallback;

        public SqlTransactionConfirmationWatchRepositoryTests()
        {
            this.dbFactory = new TestMainDatabaseFactory();
            this.subject = new SqlTransactionConfirmationWatchRepository<TestCallbackResult>(dbFactory);
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
                () => new SqlTransactionConfirmationWatchRepository<TestCallbackResult>(null)
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
            var timeout = TimeSpan.FromMinutes(5);

            var expectedDueAfter = DateTime.UtcNow.Add(timeout);

            // Act.
            var watch = await this.subject.AddAsync(transaction, 10, timeout,
                successResult, timeoutResult, this.defaultCallback, CancellationToken.None);

            // Assert.
            Assert.NotEqual(Guid.Empty, watch.Id);
            Assert.Equal(transaction, watch.Transaction);
            Assert.Equal(10, watch.Confirmation);
            Assert.True(expectedDueAfter < watch.Due);
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

            var expectedDueAfter = DateTime.UtcNow.Add(timeout);

            var watch = await this.subject.AddAsync(transaction, 10, timeout,
                successResult, timeoutResult, this.defaultCallback, CancellationToken.None);

            // Act.
            var retrieved = await this.subject.GetAsync(watch.Id, CancellationToken.None);

            // Assert.
            Assert.Equal(watch.Id, retrieved.Id);
            Assert.Equal(watch.Transaction, retrieved.Transaction);
            Assert.Equal(watch.Confirmation, retrieved.Confirmation);
            Assert.Equal(watch.Due, retrieved.Due);
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
        public async Task ListAsync_WithNonEmptyList_ShouldSuccess()
        {
            // Arrange.
            await this.CreateDefaultCallback();

            var transaction = uint256.Parse("008b3395991c7893bb8a82d8389a48ded863af914d9cc31711554bc97e4723c0");
            var successResult = new TestCallbackResult(CallbackResult.StatusSuccess, "success");
            var timeoutResult = new TestCallbackResult(CallbackResult.StatusError, "timeout");



            var watches = new List<TransactionConfirmationWatch<TestCallbackResult>>();
            watches.Add(await this.subject.AddAsync(transaction, 10, TimeSpan.FromMinutes(5),
                successResult, timeoutResult, this.defaultCallback, CancellationToken.None));

            watches.Add(await this.subject.AddAsync(transaction, 11, TimeSpan.FromHours(6),
                successResult, timeoutResult, this.defaultCallback, CancellationToken.None));

            watches = watches.Select(async watch => await this.subject.GetAsync(watch.Id, CancellationToken.None))
                .Select(watche => watche.Result)
                .ToList();

            // Act.
            var retrieved = (await this.subject.ListAsync(CancellationToken.None)).ToList();

            // Assert.
            Assert.Equal(2, retrieved.Count());
            Assert.Equal(watches[0].Id, retrieved[0].Id);
            Assert.Equal(watches[0].Transaction, retrieved[0].Transaction);
            Assert.Equal(watches[0].Confirmation, retrieved[0].Confirmation);
            Assert.Equal(watches[0].Due, retrieved[0].Due);
            Assert.Equal(watches[0].Success, retrieved[0].Success);
            Assert.Equal(watches[0].Timeout, retrieved[0].Timeout);
            Assert.Equal(watches[0].Callback, retrieved[0].Callback);

            Assert.Equal(watches[1].Id, retrieved[1].Id);
            Assert.Equal(watches[1].Callback, retrieved[1].Callback);
        }

        [Fact]
        public async Task ListAsync_WithEmptyList_ShouldReturnEmptyIEnumerable()
        {
            Assert.Empty(await this.subject.ListAsync(CancellationToken.None));
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