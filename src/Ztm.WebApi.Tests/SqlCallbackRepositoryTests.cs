using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Ztm.Data.Entity.Contexts;
using Ztm.Data.Entity.Contexts.Main;
using Ztm.Data.Entity.Testing;

namespace Ztm.WebApi.Tests
{
    public class SqlCallbackRepositoryTests
    {
        readonly ICallbackRepository subject;
        readonly IMainDatabaseFactory dbFactory;

        readonly Uri defaultUrl;

        public SqlCallbackRepositoryTests()
        {
            this.defaultUrl = new Uri("http://zcoin.io");

            this.dbFactory = new TestMainDatabaseFactory();
            this.subject = new SqlCallbackRepository(dbFactory);
        }

        [Fact]
        public async Task AddAsync_WithValidArgs_ShouldSuccess()
        {
            // Arrange.
            var registeredTime = DateTime.Now;

            // Act.
            var callback = await this.subject.AddAsync(IPAddress.Loopback, registeredTime, this.defaultUrl, CancellationToken.None);

            // Assert.
            Assert.NotEqual(Guid.Empty, callback.Id);
            Assert.Equal(IPAddress.Loopback, callback.RegisteredIp);
            Assert.Equal(registeredTime.ToUniversalTime(), callback.RegisteredTime);
            Assert.False(callback.Completed);
            Assert.Equal(this.defaultUrl, callback.Url);
        }

        [Fact]
        public async Task AddAsync_WithInvalidArgs_ShouldThrow()
        {
            var registeredTime = DateTime.Now;

            await Assert.ThrowsAsync<ArgumentNullException>(
                "ip", () => this.subject.AddAsync(null, registeredTime, this.defaultUrl, CancellationToken.None));
            await Assert.ThrowsAsync<ArgumentNullException>(
                "url", () => this.subject.AddAsync(IPAddress.Loopback, registeredTime, null, CancellationToken.None));
        }

        [Fact]
        public async Task SetStatusAsCompletedAsync_WithValidId_ShouldSuccess()
        {
            // Arrange.
            var registeredTime = DateTime.Now;
            var callback = await this.subject.AddAsync(IPAddress.Loopback, registeredTime, this.defaultUrl, CancellationToken.None);

            // Act.
            var updated = await this.subject.SetStatusAsCompletedAsync(callback.Id, CancellationToken.None);

            // Assert.
            Assert.Equal(callback.Id, updated.Id);
            Assert.Equal(IPAddress.Loopback, updated.RegisteredIp);
            Assert.Equal(registeredTime.ToUniversalTime(), updated.RegisteredTime);
            Assert.True(updated.Completed);
            Assert.Equal(this.defaultUrl, updated.Url);
        }

        [Fact]
        public async Task SetStatusAsCompletedAsync_WithNonexistentId_ShouldThrow()
        {
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => this.subject.SetStatusAsCompletedAsync(Guid.NewGuid(), CancellationToken.None));
        }

        [Fact]
        public async Task GetAsync_WithExistentId_ShouldSuccess()
        {
            // Arrange.
            var registeredTime = DateTime.Now;

            // Act.
            var callback = await this.subject.AddAsync(IPAddress.Loopback, registeredTime, this.defaultUrl, CancellationToken.None);
            var retrieved = await this.subject.GetAsync(callback.Id, CancellationToken.None);

            // Assert.
            Assert.Equal(callback.Id, retrieved.Id);
            Assert.Equal(IPAddress.Loopback, retrieved.RegisteredIp);
            Assert.Equal(registeredTime.ToUniversalTime(), retrieved.RegisteredTime);
            Assert.False(retrieved.Completed);
            Assert.Equal(this.defaultUrl, retrieved.Url);
        }

        [Fact]
        public async Task GetAsync_WithNonexistentId_ShouldReturnNull()
        {
            var retrieved = await this.subject.GetAsync(Guid.NewGuid(), CancellationToken.None);
            Assert.Null(retrieved);
        }

        [Fact]
        public async Task AddInvocation_WithExistentId_ShouldSuccess()
        {
            // Arrange.
            var registeredTime = DateTime.Now;
            var callback = await this.subject.AddAsync(IPAddress.Loopback, registeredTime, this.defaultUrl, CancellationToken.None);

            var invokedTime = registeredTime.Add(TimeSpan.FromDays(1));
            var data = Encoding.ASCII.GetBytes("txid:46bdfcc6c953ba3e9a12456e3bd75ff887c9ba50051b3c58113eebffa35d7df4");

            // Act.
            await this.subject.AddInvocationAsync(
                callback.Id, CallbackResult.StatusUpdate, invokedTime, data, CancellationToken.None);

            // Assert.
            WebApiCallbackHistory invocation;
            using (var db = this.dbFactory.CreateDbContext())
            {
                invocation = await db.WebApiCallbackHistories.FirstAsync(CancellationToken.None);
            }

            Assert.Equal(callback.Id, invocation.CallbackId);
            Assert.Equal(CallbackResult.StatusUpdate, invocation.Status);
            Assert.Equal(invokedTime.ToUniversalTime(), DateTime.SpecifyKind(invocation.InvokedTime, DateTimeKind.Utc));
            Assert.Equal(data, invocation.Data);
        }

        [Fact]
        public async Task AddInvocation_WithInvalidArgs_ShouldThrow()
        {
            var invokedTime = DateTime.Now;
            var data = Encoding.ASCII.GetBytes("txid:46bdfcc6c953ba3e9a12456e3bd75ff887c9ba50051b3c58113eebffa35d7df4");

            await Assert.ThrowsAsync<ArgumentNullException>(
                "status",
                () => this.subject.AddInvocationAsync(
                    Guid.NewGuid(), null, invokedTime, data, CancellationToken.None
                )
            );
        }

        [Fact]
        public async Task AddInvocation_WithNonexistentId_ShouldThrow()
        {
            var invokedTime = DateTime.Now;
            var data = Encoding.ASCII.GetBytes("txid:46bdfcc6c953ba3e9a12456e3bd75ff887c9ba50051b3c58113eebffa35d7df4");

            await Assert.ThrowsAsync<KeyNotFoundException>
            (
                () => this.subject.AddInvocationAsync(
                    Guid.NewGuid(), CallbackResult.StatusUpdate, invokedTime, data, CancellationToken.None
                )
            );
        }
    }
}