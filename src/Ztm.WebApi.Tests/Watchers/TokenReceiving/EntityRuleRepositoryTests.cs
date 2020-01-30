using System;
using System.Collections.ObjectModel;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using Ztm.Data.Entity.Testing;
using Ztm.Testing;
using Ztm.WebApi.AddressPools;
using Ztm.WebApi.Callbacks;
using Ztm.WebApi.Watchers.TokenReceiving;
using Ztm.Zcoin.NBitcoin.Exodus;
using Status = Ztm.Data.Entity.Contexts.Main.TokenReceivingWatcherRuleStatus;

namespace Ztm.WebApi.Tests.Watchers.TokenReceiving
{
    public sealed class EntityRuleRepositoryTests : IDisposable
    {
        readonly PropertyId property;
        readonly ReceivingAddress address;
        readonly ReceivingAddressReservation reservation;
        readonly Callback callback;
        readonly Rule rule;
        readonly TestMainDatabaseFactory db;
        readonly Mock<ICallbackRepository> callbacks;
        readonly Mock<IReceivingAddressRepository> addresses;
        readonly EntityRuleRepository subject;

        public EntityRuleRepositoryTests()
        {
            this.property = new PropertyId(3);
            this.address = new ReceivingAddress(
                Guid.NewGuid(),
                TestAddress.Regtest1,
                true,
                new Collection<ReceivingAddressReservation>());
            this.reservation = new ReceivingAddressReservation(Guid.NewGuid(), address, DateTime.Now, null);
            this.address.Reservations.Add(this.reservation);
            this.callback = new Callback(
                Guid.NewGuid(),
                IPAddress.Parse("192.168.1.2"),
                DateTime.Now,
                false,
                new Uri("http://localhost/a"));
            this.rule = new Rule(
                this.property,
                this.reservation,
                new PropertyAmount(100),
                6,
                TimeSpan.FromHours(1),
                new TokenReceivingCallback(this.callback, "timeout"));

            this.db = new TestMainDatabaseFactory();

            try
            {
                this.callbacks = new Mock<ICallbackRepository>();
                this.addresses = new Mock<IReceivingAddressRepository>();

                this.callbacks
                    .Setup(r => r.GetAsync(this.callback.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(this.callback);

                this.addresses
                    .Setup(r => r.GetReservationAsync(this.reservation.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(this.reservation);

                this.subject = new EntityRuleRepository(this.db, this.callbacks.Object, this.addresses.Object);
            }
            catch
            {
                this.db.Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            this.db.Dispose();
        }

        [Fact]
        public void Constructor_WithNullDb_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "db",
                () => new EntityRuleRepository(null, this.callbacks.Object, this.addresses.Object));
        }

        [Fact]
        public void Constructor_WithNullCallbacks_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "callbacks",
                () => new EntityRuleRepository(this.db, null, this.addresses.Object));
        }

        [Fact]
        public void Constructor_WithNullAddresses_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "addresses",
                () => new EntityRuleRepository(this.db, this.callbacks.Object, null));
        }

        [Fact]
        public Task AddAsync_WithNullRule_ShouldThrow()
        {
            return Assert.ThrowsAsync<ArgumentNullException>(
                "rule",
                () => this.subject.AddAsync(null, CancellationToken.None));
        }

        [Fact]
        public async Task AddAsync_HaveCallback_StoredRowShouldHaveCallback()
        {
            // Arrange.
            await CreateRequiredDependenciesAsync(this.rule);

            // Act.
            await this.subject.AddAsync(this.rule, CancellationToken.None);

            // Assert.
            var row = await LoadRuleAsync(this.rule.Id);

            Assert.Equal(this.rule.Callback.Callback.Id, row.CallbackId);
            Assert.Equal(this.rule.Property.Value, row.PropertyId);
            Assert.Equal(this.rule.AddressReservation.Id, row.AddressReservationId);
            Assert.Equal(this.rule.TargetAmount.Indivisible, row.TargetAmount);
            Assert.Equal(this.rule.TargetConfirmation, row.TargetConfirmation);
            Assert.Equal(this.rule.OriginalTimeout, row.OriginalTimeout);
            Assert.Equal(this.rule.OriginalTimeout, row.CurrentTimeout);
            Assert.Equal(this.rule.Callback.TimeoutStatus, row.TimeoutStatus);
            Assert.Equal(Status.Uncompleted, row.Status);
        }

        [Fact]
        public async Task AddAsync_NoCallback_StoredRowShouldHaveNoCallback()
        {
            // Arrange.
            var rule = new Rule(
                this.property,
                this.reservation,
                new PropertyAmount(100),
                6,
                TimeSpan.FromHours(1),
                null);

            await CreateRequiredDependenciesAsync(rule);

            // Act.
            await this.subject.AddAsync(rule, CancellationToken.None);

            // Assert.
            var row = await LoadRuleAsync(rule.Id);

            Assert.Null(row.CallbackId);
            Assert.Equal(rule.Property.Value, row.PropertyId);
            Assert.Equal(rule.AddressReservation.Id, row.AddressReservationId);
            Assert.Equal(rule.TargetAmount.Indivisible, row.TargetAmount);
            Assert.Equal(rule.TargetConfirmation, row.TargetConfirmation);
            Assert.Equal(rule.OriginalTimeout, row.OriginalTimeout);
            Assert.Equal(rule.OriginalTimeout, row.CurrentTimeout);
            Assert.Null(row.TimeoutStatus);
            Assert.Equal(Status.Uncompleted, row.Status);
        }

        [Fact]
        public Task DecreaseTimeoutAsync_WithInvalidId_ShouldThrow()
        {
            return Assert.ThrowsAsync<ArgumentException>(
                "id",
                () => this.subject.DecreaseTimeoutAsync(Guid.NewGuid(), TimeSpan.FromHours(1), CancellationToken.None));
        }

        [Fact]
        public async Task DecreaseTimeoutAsync_WithLessValue_StoredRowShouldDecreasedByThatValue()
        {
            // Arrange.
            var value = this.rule.OriginalTimeout / 2;

            await CreateRequiredDependenciesAsync(this.rule);
            await this.subject.AddAsync(this.rule, CancellationToken.None);

            // Act.
            await this.subject.DecreaseTimeoutAsync(this.rule.Id, value, CancellationToken.None);

            // Assert.
            var row = await LoadRuleAsync(this.rule.Id);

            Assert.Equal(value, row.CurrentTimeout);
            Assert.Equal(this.rule.OriginalTimeout, row.OriginalTimeout);
        }

        [Fact]
        public async Task DecreaseTimeoutAsync_WithGreaterValue_StoredRowShouldBeZero()
        {
            // Arrange.
            var value = this.rule.OriginalTimeout + TimeSpan.FromMilliseconds(1);

            await CreateRequiredDependenciesAsync(this.rule);
            await this.subject.AddAsync(this.rule, CancellationToken.None);

            // Act.
            await this.subject.DecreaseTimeoutAsync(this.rule.Id, value, CancellationToken.None);

            // Assert.
            var row = await LoadRuleAsync(this.rule.Id);

            Assert.Equal(TimeSpan.Zero, row.CurrentTimeout);
            Assert.Equal(this.rule.OriginalTimeout, row.OriginalTimeout);
        }

        [Fact]
        public async Task GetAsync_WithInvalidId_ShouldReturnNull()
        {
            // Arrange.
            await CreateRequiredDependenciesAsync(this.rule);
            await this.subject.AddAsync(this.rule, CancellationToken.None);

            // Act.
            var result = await this.subject.GetAsync(Guid.NewGuid(), CancellationToken.None);

            // Assert.
            Assert.Null(result);
        }

        [Fact]
        public async Task GetAsync_WithValidId_ShouldReturnCorrespondingObject()
        {
            // Arrange.
            await CreateRequiredDependenciesAsync(this.rule);
            await this.subject.AddAsync(this.rule, CancellationToken.None);

            // Act.
            var result = await this.subject.GetAsync(this.rule.Id, CancellationToken.None);

            // Assert.
            Assert.Equal(this.rule.AddressReservation, result.AddressReservation);
            Assert.Equal(this.rule.Callback, result.Callback);
            Assert.Equal(this.rule.OriginalTimeout, result.OriginalTimeout);
            Assert.Equal(this.rule.Property, result.Property);
            Assert.Equal(this.rule.TargetAmount, result.TargetAmount);
            Assert.Equal(this.rule.TargetConfirmation, result.TargetConfirmation);
        }

        [Fact]
        public Task GetCurrentTimeoutAsync_WithInvalidId_ShouldThrow()
        {
            return Assert.ThrowsAsync<ArgumentException>(
                "id",
                () => this.subject.GetCurrentTimeoutAsync(Guid.NewGuid(), CancellationToken.None));
        }

        [Fact]
        public async Task GetCurrentTimeoutAsync_WithValidId_ShouldReturnCurrentTimeout()
        {
            // Arrange.
            var expected = this.rule.OriginalTimeout / 2;

            await CreateRequiredDependenciesAsync(this.rule);
            await this.subject.AddAsync(this.rule, CancellationToken.None);
            await this.subject.DecreaseTimeoutAsync(this.rule.Id, expected, CancellationToken.None);

            // Act.
            var result = await this.subject.GetCurrentTimeoutAsync(this.rule.Id, CancellationToken.None);

            // Assert.
            Assert.Equal(expected, result);
        }

        [Fact]
        public Task ListUncompletedAsync_WithNullProperty_ShouldThrow()
        {
            return Assert.ThrowsAsync<ArgumentNullException>(
                "property",
                () => this.subject.ListUncompletedAsync(null, CancellationToken.None));
        }

        [Fact]
        public async Task ListUncompletedAsync_WithNonNullProperty_ShouldReturnUncompletedForThatProperty()
        {
            // Arrange.
            var property1 = new PropertyId(3);
            var property2 = new PropertyId(4);

            var address1 = new ReceivingAddress(
                Guid.NewGuid(),
                TestAddress.Regtest1,
                true,
                new Collection<ReceivingAddressReservation>());

            var address2 = new ReceivingAddress(
                Guid.NewGuid(),
                TestAddress.Regtest2,
                true,
                new Collection<ReceivingAddressReservation>());

            var address3 = new ReceivingAddress(
                Guid.NewGuid(),
                TestAddress.Regtest3,
                true,
                new Collection<ReceivingAddressReservation>());

            var reservation1 = new ReceivingAddressReservation(Guid.NewGuid(), address1, DateTime.Now, null);
            var reservation2 = new ReceivingAddressReservation(Guid.NewGuid(), address2, DateTime.Now, DateTime.Now);
            var reservation3 = new ReceivingAddressReservation(Guid.NewGuid(), address2, DateTime.Now, DateTime.Now);
            var reservation4 = new ReceivingAddressReservation(Guid.NewGuid(), address2, DateTime.Now, null);
            var reservation5 = new ReceivingAddressReservation(Guid.NewGuid(), address3, DateTime.Now, null);

            address1.Reservations.Add(reservation1);
            address2.Reservations.Add(reservation2);
            address2.Reservations.Add(reservation3);
            address2.Reservations.Add(reservation4);
            address3.Reservations.Add(reservation5);

            var callback1 = new Callback(
                Guid.NewGuid(),
                IPAddress.Parse("192.168.1.2"),
                DateTime.Now,
                false,
                new Uri("http://localhost/a"));

            var rule1 = new Rule(
                property1,
                reservation1,
                new PropertyAmount(1),
                1,
                TimeSpan.FromMinutes(1),
                null);

            var rule2 = new Rule(
                property2,
                reservation2,
                new PropertyAmount(2),
                2,
                TimeSpan.FromMinutes(2),
                null);

            var rule3 = new Rule(
                property2,
                reservation3,
                new PropertyAmount(3),
                3,
                TimeSpan.FromMinutes(3),
                null);

            var rule4 = new Rule(
                property2,
                reservation4,
                new PropertyAmount(4),
                4,
                TimeSpan.FromMinutes(4),
                new TokenReceivingCallback(callback1, "1"));

            var rule5 = new Rule(
                property2,
                reservation5,
                new PropertyAmount(5),
                5,
                TimeSpan.FromMinutes(5),
                null);

            await CreateAddressAsync(address1);
            await CreateAddressAsync(address2);
            await CreateAddressAsync(address3);

            await CreateAddressReservationAsync(reservation1);
            await CreateAddressReservationAsync(reservation2);
            await CreateAddressReservationAsync(reservation3);
            await CreateAddressReservationAsync(reservation4);
            await CreateAddressReservationAsync(reservation5);

            await CreateCallbackAsync(callback1);

            await this.subject.AddAsync(rule1, CancellationToken.None);
            await this.subject.AddAsync(rule2, CancellationToken.None);
            await this.subject.AddAsync(rule3, CancellationToken.None);
            await this.subject.AddAsync(rule4, CancellationToken.None);
            await this.subject.AddAsync(rule5, CancellationToken.None);

            await this.subject.SetSucceededAsync(rule2.Id, CancellationToken.None);
            await this.subject.SetTimedOutAsync(rule3.Id, CancellationToken.None);

            this.addresses
                .Setup(r => r.GetReservationAsync(reservation1.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(reservation1);

            this.addresses
                .Setup(r => r.GetReservationAsync(reservation2.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(reservation2);

            this.addresses
                .Setup(r => r.GetReservationAsync(reservation3.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(reservation3);

            this.addresses
                .Setup(r => r.GetReservationAsync(reservation4.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(reservation4);

            this.addresses
                .Setup(r => r.GetReservationAsync(reservation5.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(reservation5);

            this.callbacks
                .Setup(r => r.GetAsync(callback1.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(callback1);

            // Act.
            var result = await this.subject.ListUncompletedAsync(property2, CancellationToken.None);

            // Assert.
            Assert.Collection(
                result,
                r =>
                {
                    Assert.Equal(rule4.AddressReservation, r.AddressReservation);
                    Assert.Equal(rule4.Callback, r.Callback);
                    Assert.Equal(rule4.Id, r.Id);
                    Assert.Equal(rule4.OriginalTimeout, r.OriginalTimeout);
                    Assert.Equal(rule4.Property, r.Property);
                    Assert.Equal(rule4.TargetAmount, r.TargetAmount);
                    Assert.Equal(rule4.TargetConfirmation, r.TargetConfirmation);
                },
                r =>
                {
                    Assert.Equal(rule5.AddressReservation, r.AddressReservation);
                    Assert.Null(r.Callback);
                    Assert.Equal(rule5.Id, r.Id);
                    Assert.Equal(rule5.OriginalTimeout, r.OriginalTimeout);
                    Assert.Equal(rule5.Property, r.Property);
                    Assert.Equal(rule5.TargetAmount, r.TargetAmount);
                    Assert.Equal(rule5.TargetConfirmation, r.TargetConfirmation);
                });
        }

        [Fact]
        public Task SetSucceededAsync_WithInvalidId_ShouldThrow()
        {
            return Assert.ThrowsAsync<ArgumentException>(
                "id",
                () => this.subject.SetSucceededAsync(Guid.NewGuid(), CancellationToken.None));
        }

        [Fact]
        public async Task SetSucceededAsync_WithValidId_StoredRowShouldChangedToSucceeded()
        {
            // Arrange.
            await CreateRequiredDependenciesAsync(this.rule);
            await this.subject.AddAsync(this.rule, CancellationToken.None);

            // Act.
            await this.subject.SetSucceededAsync(this.rule.Id, CancellationToken.None);

            // Assert.
            var row = await LoadRuleAsync(this.rule.Id);

            Assert.Equal(Status.Succeeded, row.Status);
        }

        [Fact]
        public Task SetTimedOutAsync_WithInvalidId_ShouldThrow()
        {
            return Assert.ThrowsAsync<ArgumentException>(
                "id",
                () => this.subject.SetTimedOutAsync(Guid.NewGuid(), CancellationToken.None));
        }

        [Fact]
        public async Task SetTimedOutAsync_WithValidId_StoredRowShouldChangedToTimedOut()
        {
            // Arrange.
            await CreateRequiredDependenciesAsync(this.rule);
            await this.subject.AddAsync(this.rule, CancellationToken.None);

            // Act.
            await this.subject.SetTimedOutAsync(this.rule.Id, CancellationToken.None);

            // Assert.
            var row = await LoadRuleAsync(this.rule.Id);

            Assert.Equal(Status.TimedOut, row.Status);
        }

        async Task<Ztm.Data.Entity.Contexts.Main.TokenReceivingWatcherRule> LoadRuleAsync(Guid id)
        {
            using (var db = this.db.CreateDbContext())
            {
                return await db.TokenReceivingWatcherRules.SingleAsync(e => e.Id == id);
            }
        }

        async Task CreateRequiredDependenciesAsync(Rule rule)
        {
            await CreateAddressAsync(rule.AddressReservation.Address);
            await CreateAddressReservationAsync(rule.AddressReservation);

            if (rule.Callback != null)
            {
                await CreateCallbackAsync(rule.Callback.Callback);
            }
        }

        async Task CreateAddressAsync(ReceivingAddress address)
        {
            var entity = new Ztm.Data.Entity.Contexts.Main.ReceivingAddress()
            {
                Id = address.Id,
                Address = address.Address.ToString(),
                IsLocked = address.IsLocked,
            };

            using (var db = this.db.CreateDbContext())
            {
                await db.ReceivingAddresses.AddAsync(entity);
                await db.SaveChangesAsync();
            }
        }

        async Task CreateAddressReservationAsync(ReceivingAddressReservation reservation)
        {
            var entity = new Ztm.Data.Entity.Contexts.Main.ReceivingAddressReservation()
            {
                Id = reservation.Id,
                AddressId = reservation.Address.Id,
                LockedAt = reservation.ReservedDate.ToUniversalTime(),
                ReleasedAt = null,
            };

            using (var db = this.db.CreateDbContext())
            {
                await db.ReceivingAddressReservations.AddAsync(entity);
                await db.SaveChangesAsync();
            }
        }

        async Task CreateCallbackAsync(Callback callback)
        {
            var entity = new Ztm.Data.Entity.Contexts.Main.WebApiCallback()
            {
                Id = callback.Id,
                RegisteredIp = callback.RegisteredIp,
                RegisteredTime = callback.RegisteredTime.ToUniversalTime(),
                Completed = callback.Completed,
                Url = callback.Url,
            };

            using (var db = this.db.CreateDbContext())
            {
                await db.WebApiCallbacks.AddAsync(entity);
                await db.SaveChangesAsync();
            }
        }
    }
}
