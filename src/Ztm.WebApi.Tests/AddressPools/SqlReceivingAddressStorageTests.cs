using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using Xunit;
using Ztm.Data.Entity.Testing;
using Ztm.Testing;
using Ztm.WebApi.AddressPools;
using Ztm.Zcoin.NBitcoin;

namespace Ztm.WebApi.Tests.AddressPools
{
    public sealed class SqlReceivingAddressStorageTests : IDisposable
    {
        readonly TestMainDatabaseFactory databaseFactory;
        readonly Network network;

        readonly SqlReceivingAddressStorage subject;

        public SqlReceivingAddressStorageTests()
        {
            this.databaseFactory = new TestMainDatabaseFactory();
            this.network = ZcoinNetworks.Instance.Regtest;

            this.subject = new SqlReceivingAddressStorage(this.databaseFactory, this.network);
        }

        public void Dispose()
        {
            this.databaseFactory.Dispose();
        }

        [Fact]
        public void Construct_WithNullArgs_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "databaseFactory",
                () => new SqlReceivingAddressStorage(null, this.network)
            );

            Assert.Throws<ArgumentNullException>(
                "network",
                () => new SqlReceivingAddressStorage(this.databaseFactory, null)
            );
        }

        [Fact]
        public async Task AddAddressAsync_WithValidArgs_ShouldSuccess()
        {
            // Arrange.
            var address = TestAddress.Regtest1;

            // Act.
            var result = await this.subject.AddAddressAsync(TestAddress.Regtest1, CancellationToken.None);

            // Assert.
            Assert.NotEqual(Guid.Empty, result.Id);
            Assert.Equal(address, result.Address);
            Assert.Null(result.ReceivingAddressReservations);
        }

        [Fact]
        public void AddAddressAsync_WithNullAddress_ShouldThrow()
        {
            _ = Assert.ThrowsAsync<ArgumentNullException>(
                "address",
                () => this.subject.AddAddressAsync(null, CancellationToken.None)
            );
        }

        [Fact]
        public async Task GetAsync_ExistReceivingAddress_ShouldSuccess()
        {
            // Arrange.
            var recv = await this.subject.AddAddressAsync(TestAddress.Regtest1, CancellationToken.None);

            // Act.
            var received = await this.subject.GetAsync(recv.Id, CancellationToken.None);

            // Assert.
            Assert.Equal(recv, received);
        }

        [Fact]
        public async Task ListReceivingAddressAsync_WithEmptyRecerivingAddress_ShouldGetEmpty()
        {
            // Act.
            var result = await this.subject.ListReceivingAddressAsync(CancellationToken.None);

            // Assert.
            Assert.Empty(result);
        }

        [Fact]
        public async Task ListReceivingAddressAsync_WithNonEmptyRecerivingAddress_ShouldGetRecAddresses()
        {
            // Arrange.
            var receivingAddresses = new List<ReceivingAddress>
            {
                await this.subject.AddAddressAsync(TestAddress.Regtest1, CancellationToken.None),
                await this.subject.AddAddressAsync(TestAddress.Regtest2, CancellationToken.None)
            };

            // Act.
            var result = await this.subject.ListReceivingAddressAsync(CancellationToken.None);

            // Assert.
            Assert.Equal(receivingAddresses, result.ToList());
        }

        [Fact]
        public async Task CreateReservationAsync_WithExistAddress_ShouldSuccess()
        {
            // Arrange.
            var address = await this.subject.AddAddressAsync(TestAddress.Regtest1, CancellationToken.None);
            var startedTime = DateTime.UtcNow;

            // Act.
            var r = await this.subject.CreateReservationAsync(address.Id, CancellationToken.None);
            var a = await this.subject.GetAsync(address.Id, CancellationToken.None);

            // Assert.
            Assert.NotEqual(Guid.Empty, r.Id);
            Assert.True(startedTime < r.ReservedDate);
            Assert.Null(r.ReleasedDate);

            Assert.Single(a.ReceivingAddressReservations);
            Assert.Equal(r.Id, a.ReceivingAddressReservations.First().Id);
        }

        [Fact]
        public async Task GetReservationAsync_WithExistsReservation_ShouldSuccess()
        {
            // Arrange.
            var address = await this.subject.AddAddressAsync(TestAddress.Regtest1, CancellationToken.None);
            var startedTime = DateTime.UtcNow;
            var r = await this.subject.CreateReservationAsync(address.Id, CancellationToken.None);

            // Act.
            var retreived = await this.subject.GetReservationAsync(r.Id, CancellationToken.None);

            // Assert.
            Assert.Equal(r.Id, retreived.Id);
            Assert.Equal(r.ReservedDate, retreived.ReservedDate);
            Assert.Null(retreived.ReleasedDate);
        }

        [Fact]
        public async Task SetReleasedTimeAsync_WithValidId_ShouldSuccess()
        {
            // Arrange.
            var address = await this.subject.AddAddressAsync(TestAddress.Regtest1, CancellationToken.None);
            var startedTime = DateTime.UtcNow;
            var r = await this.subject.CreateReservationAsync(address.Id, CancellationToken.None);

            // Act.
            await this.subject.SetReleasedTimeAsync(r.Id,CancellationToken.None);
            var updated = await this.subject.GetReservationAsync(r.Id, CancellationToken.None);

            // Assert.
            Assert.NotNull(updated.ReleasedDate);
            Assert.True(startedTime < updated.ReleasedDate);
        }
    }
}