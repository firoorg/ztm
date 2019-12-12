using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NBitcoin;
using Xunit;
using Ztm.Data.Entity.Testing;
using Ztm.Testing;
using Ztm.WebApi.AddressPools;
using Ztm.Zcoin.NBitcoin;

namespace Ztm.WebApi.Tests.AddressPools
{
    public sealed class EntityReceivingAddressStorageTests : IDisposable
    {
        readonly TestMainDatabaseFactory databaseFactory;
        readonly Network network;

        readonly EntityReceivingAddressStorage subject;

        public EntityReceivingAddressStorageTests()
        {
            this.databaseFactory = new TestMainDatabaseFactory();
            this.network = ZcoinNetworks.Instance.Regtest;

            this.subject = new EntityReceivingAddressStorage(this.databaseFactory, this.network);
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
                () => new EntityReceivingAddressStorage(null, this.network)
            );

            Assert.Throws<ArgumentNullException>(
                "network",
                () => new EntityReceivingAddressStorage(this.databaseFactory, null)
            );
        }

        [Fact]
        public async Task AddAsync_WithValidArgs_ShouldSuccess()
        {
            // Arrange.
            var address = TestAddress.Regtest1;

            // Act.
            var result = await this.subject.AddAsync(TestAddress.Regtest1, CancellationToken.None);

            // Assert.
            Assert.NotEqual(Guid.Empty, result.Id);
            Assert.Equal(address, result.Address);
            Assert.Empty(result.Reservations);

            using (var db = this.databaseFactory.CreateDbContext())
            {
                var receivingAddress = await db.ReceivingAddresses.FirstOrDefaultAsync(a => a.Id == result.Id);
                Assert.NotNull(receivingAddress);
                Assert.Equal(result.Id, receivingAddress.Id);
            }
        }

        [Fact]
        public async Task AddAsync_WithNullAddress_ShouldThrow()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                "address",
                () => this.subject.AddAsync(null, CancellationToken.None)
            );
        }

        [Fact]
        public async Task AddAsync_SameAddressTwice_ShouldThrow()
        {
            var address = TestAddress.Regtest1;
            await this.subject.AddAsync(TestAddress.Regtest1, CancellationToken.None);

            await Assert.ThrowsAsync<ArgumentException>(
                "address",
                () => this.subject.AddAsync(TestAddress.Regtest1, CancellationToken.None));
        }

        [Fact]
        public async Task GetAsync_WithNonExist_ShouldReturnNull()
        {
            Assert.Null(await this.subject.GetAsync(Guid.NewGuid(), CancellationToken.None));
        }

        [Fact]
        public async Task GetAsync_ExistReceivingAddress_ShouldSuccess()
        {
            // Arrange.
            var recv = await this.subject.AddAsync(TestAddress.Regtest1, CancellationToken.None);

            // Act.
            var received = await this.subject.GetAsync(recv.Id, CancellationToken.None);

            // Assert.
            Assert.Equal(recv, received);
        }

        [Fact]
        public async Task ListAsync_WithEmptyRecerivingAddress_ShouldReturnEmpty()
        {
            // Act.
            var result = await this.subject.ListAsync(AddressFilter.None, CancellationToken.None);

            // Assert.
            Assert.Empty(result);
        }

        [Fact]
        public async Task ListAsync_WithNonEmptyRecerivingAddress_ShouldGetRecAddresses()
        {
            // Arrange.
            var receivingAddresses = new List<ReceivingAddress>
            {
                await this.subject.AddAsync(TestAddress.Regtest1, CancellationToken.None),
                await this.subject.AddAsync(TestAddress.Regtest2, CancellationToken.None)
            };

            // Act.
            var result = await this.subject.ListAsync(AddressFilter.None, CancellationToken.None);

            // Assert.
            Assert.Subset(result.ToHashSet(), receivingAddresses.ToHashSet());
        }

        [Fact]
        public async Task ListAsync_WithAvailableFlag_ShouldAllAvailableAddresses()
        {
            // Arrange.
            var receivingAddresses = new List<ReceivingAddress>
            {
                await this.subject.AddAsync(TestAddress.Regtest1, CancellationToken.None),
                await this.subject.AddAsync(TestAddress.Regtest2, CancellationToken.None)
            };
            await this.subject.TryLockAsync(receivingAddresses.First().Id, CancellationToken.None);

            // Act.
            var result = await this.subject.ListAsync(AddressFilter.Available, CancellationToken.None);

            // Assert.
            Assert.Single(result);
            Assert.Equal(receivingAddresses.Last().Id, result.First().Id);
        }

        [Fact]
        public async Task ListAsync_WithNeverUsedFlag_ShouldAllFreshAddresses()
        {
            // Arrange.
            var receivingAddresses = new List<ReceivingAddress>
            {
                await this.subject.AddAsync(TestAddress.Regtest1, CancellationToken.None),
                await this.subject.AddAsync(TestAddress.Regtest2, CancellationToken.None)
            };
            var r = await this.subject.TryLockAsync(receivingAddresses.First().Id, CancellationToken.None);
            await this.subject.ReleaseAsync(r.Id, CancellationToken.None);

            // Act.
            var availables = await this.subject.ListAsync(AddressFilter.Available, CancellationToken.None);
            var neverUsed = await this.subject.ListAsync(AddressFilter.NeverUsed, CancellationToken.None);

            // Assert.
            Assert.Equal(2, availables.Count());
            Assert.Single(neverUsed);
            Assert.Equal(receivingAddresses.Last().Id, neverUsed.First().Id);
        }

        [Fact]
        public async Task TryLockAsync_WithAvailableExist_ShouldSuccess()
        {
            // Arrange.
            var result = await this.subject.AddAsync(TestAddress.Regtest1, CancellationToken.None);
            var startedAt = DateTime.UtcNow;

            // Act.
            var reservation = await this.subject.TryLockAsync(result.Id, CancellationToken.None);
            var recv = await this.subject.GetAsync(result.Id, CancellationToken.None);

            // Assert.
            Assert.NotNull(reservation);

            Assert.NotEqual(Guid.Empty, reservation.Id);
            Assert.Equal(result.Id, reservation.Address.Id);

            Assert.True(startedAt < reservation.ReservedDate);

            Assert.Null(reservation.ReleasedDate);

            Assert.Single(recv.Reservations);
            Assert.Equal(reservation.Id, recv.Reservations.First().Id);
        }

        [Fact]
        public async Task TryLockAsync_Locked_ShouldReturnNull()
        {
            // Arrange.
            var result = await this.subject.AddAsync(TestAddress.Regtest1, CancellationToken.None);

            // Act.
            var reservation = await this.subject.TryLockAsync(result.Id, CancellationToken.None);
            var anotherReservation = await this.subject.TryLockAsync(result.Id, CancellationToken.None);

            var recv = await this.subject.GetAsync(result.Id, CancellationToken.None);

            // Assert.
            Assert.NotNull(reservation);
            Assert.Null(anotherReservation);

            Assert.Single(recv.Reservations);
            Assert.Equal(reservation.Id, recv.Reservations.First().Id);
        }

        [Fact]
        public async Task ReleaseAsync_WithLocked_ShouldUnlock()
        {
            // Arrange.
            var address = await this.subject.AddAsync(TestAddress.Regtest1, CancellationToken.None);
            var reservation = await this.subject.TryLockAsync(address.Id, CancellationToken.None);

            // Act.
            await this.subject.ReleaseAsync(reservation.Id, CancellationToken.None);
            var unlockedRecv = await this.subject.GetAsync(address.Id, CancellationToken.None);

            // Assert.
            Assert.Single(unlockedRecv.Reservations);
            var updatedResevation = unlockedRecv.Reservations.First();

            Assert.True(updatedResevation.ReservedDate < updatedResevation.ReleasedDate);
            Assert.False(unlockedRecv.IsLocked);
        }

        [Fact]
        public async Task ReleaseAsync_Released_ShouldThrow()
        {
            // Arrange.
            var address = await this.subject.AddAsync(TestAddress.Regtest1, CancellationToken.None);
            var reservation = await this.subject.TryLockAsync(address.Id, CancellationToken.None);
            await this.subject.ReleaseAsync(reservation.Id, CancellationToken.None);

            // Act & Assert.
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => this.subject.ReleaseAsync(reservation.Id, CancellationToken.None)
            );
        }

        [Fact]
        public async Task ReleaseAsync_InExist_ShouldThrow()
        {
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => this.subject.ReleaseAsync(Guid.NewGuid(), CancellationToken.None)
            );
        }

        [Fact]
        public async Task TryLockAsync_NonExistAddres_ShouldThrow()
        {
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => this.subject.TryLockAsync(Guid.NewGuid(), CancellationToken.None));
        }

        [Fact]
        public async Task TryLockAsync_ReleasedAddress_ShouldSuccess()
        {
            // Arrange.
            var address = await this.subject.AddAsync(TestAddress.Regtest1, CancellationToken.None);
            var reservation = await this.subject.TryLockAsync(address.Id, CancellationToken.None);
            await this.subject.ReleaseAsync(reservation.Id, CancellationToken.None);

            // Act.
            var newReservation = await this.subject.TryLockAsync(address.Id, CancellationToken.None);
            var lockedAddress = await this.subject.GetAsync(address.Id, CancellationToken.None);

            // Assert.
            Assert.NotNull(lockedAddress);
            Assert.True(lockedAddress.IsLocked);
            Assert.Equal(2, lockedAddress.Reservations.Count);

            Assert.NotEmpty(lockedAddress.Reservations.Where(r => r.ReleasedDate == null));
        }
    }
}