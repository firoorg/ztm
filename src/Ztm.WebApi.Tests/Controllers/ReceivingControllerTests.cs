using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using Ztm.Testing;
using Ztm.WebApi.AddressPools;
using Ztm.WebApi.ApiExceptions;
using Ztm.WebApi.Callbacks;
using Ztm.WebApi.Controllers;
using Ztm.WebApi.Models;
using Ztm.WebApi.Watchers.TokenReceiving;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.WebApi.Tests.Controllers
{
    public sealed class ReceivingControllerTests : ControllerTesting<ReceivingController>
    {
        readonly ReceivingAddress address;
        readonly ReceivingAddressReservation reservation;
        readonly IConfiguration config;
        readonly Mock<IReceivingAddressPool> pool;
        readonly Mock<ITokenReceivingWatcher> watcher;

        public ReceivingControllerTests()
        {
            this.address = new ReceivingAddress(
                Guid.NewGuid(),
                TestAddress.Regtest1,
                true,
                new Collection<ReceivingAddressReservation>());

            this.reservation = new ReceivingAddressReservation(Guid.NewGuid(), this.address, DateTime.Now, null);

            this.address.Reservations.Add(this.reservation);

            this.config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>()
            {
                {"Api:Default:RequiredConfirmation", "6"},
                {"Api:Default:TransactionTimeout", "1:00:00"},
            }).Build();

            this.pool = new Mock<IReceivingAddressPool>();
            this.watcher = new Mock<ITokenReceivingWatcher>();
        }

        [Fact]
        public void Constructor_WithNullConfig_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "config",
                () => new ReceivingController(null, this.pool.Object, this.watcher.Object, Helper));
        }

        [Fact]
        public void Constructor_WithNullPool_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "pool",
                () => new ReceivingController(this.config, null, this.watcher.Object, Helper));
        }

        [Fact]
        public void Constructor_WithNullWatcher_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "watcher",
                () => new ReceivingController(this.config, this.pool.Object, null, Helper));
        }

        [Fact]
        public void Constructor_WithNullHelper_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "helper",
                () => new ReceivingController(this.config, this.pool.Object, this.watcher.Object, null));
        }

        [Fact]
        public Task PostAsync_NoAvailableAddress_ShouldServiceUnavailable()
        {
            return AsynchronousTesting.WithCancellationTokenAsync(async cancellationToken =>
            {
                // Arrange.
                var request = new ReceivingRequest()
                {
                    TargetAmount = new PropertyAmount(100),
                };

                this.pool
                    .Setup(p => p.TryLockAddressAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync((ReceivingAddressReservation)null);

                // Act.
                var result = await Subject.PostAsync(request, cancellationToken);

                // Assert.
                var status = Assert.IsType<StatusCodeResult>(result);

                Assert.Equal((int)HttpStatusCode.ServiceUnavailable, status.StatusCode);

                this.pool.Verify(p => p.TryLockAddressAsync(cancellationToken), Times.Once());
            });
        }

        [Fact]
        public Task PostAsync_NoCallback_ShouldNotWatchWithCallback()
        {
            return AsynchronousTesting.WithCancellationTokenAsync(async cancellationToken =>
            {
                // Arrange.
                var request = new ReceivingRequest()
                {
                    TargetAmount = new PropertyAmount(100),
                };

                this.pool
                    .Setup(p => p.TryLockAddressAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(this.reservation);

                // Act.
                var result = await Subject.PostAsync(request, cancellationToken);

                // Assert.
                var accepted = Assert.IsType<AcceptedResult>(result);
                var response = Assert.IsType<ReceivingResponse>(accepted.Value);

                Assert.Null(accepted.Location);
                Assert.Equal(this.address.Address, response.Address);
                Assert.Equal(DateTimeKind.Utc, response.Deadline.Kind);
                Assert.Equal(DateTime.UtcNow + TimeSpan.FromHours(1), response.Deadline, TimeSpan.FromSeconds(1));

                this.pool.Verify(p => p.TryLockAddressAsync(cancellationToken), Times.Once());

                this.Callbacks.Verify(
                    r => r.AddAsync(It.IsAny<IPAddress>(), It.IsAny<Uri>(), It.IsAny<CancellationToken>()),
                    Times.Never());

                this.watcher.Verify(
                    w => w.StartWatchAsync(
                        this.reservation,
                        new PropertyAmount(100),
                        6,
                        TimeSpan.FromHours(1),
                        null,
                        CancellationToken.None),
                    Times.Once());

                this.pool.Verify(
                    p => p.ReleaseAddressAsync(this.reservation.Id, It.IsAny<CancellationToken>()),
                    Times.Never());
            });
        }

        [Fact]
        public Task PostAsync_WithCallback_ShouldWatchWithCallback()
        {
            return AsynchronousTesting.WithCancellationTokenAsync(async cancellationToken =>
            {
                // Arrange.
                var request = new ReceivingRequest()
                {
                    TargetAmount = new PropertyAmount(1000),
                };

                var callback = new Callback(
                    Guid.NewGuid(),
                    IPAddress.Parse("192.168.1.2"),
                    DateTime.Now,
                    false,
                    new Uri("http://localhost/a"));

                this.pool
                    .Setup(p => p.TryLockAddressAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(this.reservation);

                MockCallback(callback);

                // Act.
                var result = await Subject.PostAsync(request, cancellationToken);

                // Assert.
                var callbackId = Assert.Contains(CallbackId, ResponseHeaders);
                var accepted = Assert.IsType<AcceptedResult>(result);
                var response = Assert.IsType<ReceivingResponse>(accepted.Value);

                Assert.Equal(callback.Id.ToString(), callbackId);
                Assert.Null(accepted.Location);
                Assert.Equal(this.address.Address, response.Address);
                Assert.Equal(DateTime.UtcNow + TimeSpan.FromHours(1), response.Deadline, TimeSpan.FromSeconds(1));

                this.pool.Verify(p => p.TryLockAddressAsync(cancellationToken), Times.Once());

                this.Callbacks.Verify(
                    r => r.AddAsync(callback.RegisteredIp, callback.Url, CancellationToken.None),
                    Times.Once());

                this.watcher.Verify(
                    w => w.StartWatchAsync(
                        this.reservation,
                        new PropertyAmount(1000),
                        6,
                        TimeSpan.FromHours(1),
                        new TokenReceivingCallback(callback, ReceivingController.TimeoutStatus),
                        CancellationToken.None),
                    Times.Once());

                this.pool.Verify(
                    p => p.ReleaseAddressAsync(this.reservation.Id, It.IsAny<CancellationToken>()),
                    Times.Never());
            });
        }

        [Theory]
        [InlineData("abc")]
        [InlineData("ftp://localhost")]
        public Task PostAsync_WithInvalidCallbackUrl_ShouldReleaseAddressReservation(string url)
        {
            return AsynchronousTesting.WithCancellationTokenAsync(async cancellationToken =>
            {
                // Arrange.
                var request = new ReceivingRequest()
                {
                    TargetAmount = new PropertyAmount(1),
                };

                RequestHeaders.Add(CallbackUrl, url);

                this.pool
                    .Setup(p => p.TryLockAddressAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(this.reservation);

                // Act.
                await Assert.ThrowsAsync<InvalidCallbackUrlException>(
                    () => Subject.PostAsync(request, cancellationToken));

                // Assert.
                this.pool.Verify(p => p.TryLockAddressAsync(cancellationToken), Times.Once());

                this.watcher.Verify(
                    w => w.StartWatchAsync(
                        It.IsAny<ReceivingAddressReservation>(),
                        It.IsAny<PropertyAmount>(),
                        It.IsAny<int>(),
                        It.IsAny<TimeSpan>(),
                        It.IsAny<TokenReceivingCallback>(),
                        It.IsAny<CancellationToken>()),
                        Times.Never());

                this.pool.Verify(p => p.ReleaseAddressAsync(this.reservation.Id, CancellationToken.None), Times.Once());
            });
        }

        [Fact]
        public Task PostAsync_ErrorWhileStoringCallback_ShouldReleaseAddressReservation()
        {
            return AsynchronousTesting.WithCancellationTokenAsync(async cancellationToken =>
            {
                // Arrange.
                var requester = IPAddress.Parse("192.168.1.2");
                var callback = new Uri("http://localhost/a");
                var ex = new Exception();

                var request = new ReceivingRequest()
                {
                    TargetAmount = new PropertyAmount(10000),
                };

                RequestHeaders.Add(CallbackUrl, callback.ToString());
                Connection.SetupGet(c => c.RemoteIpAddress).Returns(requester);

                this.pool
                    .Setup(p => p.TryLockAddressAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(this.reservation);

                this.Callbacks
                    .Setup(r => r.AddAsync(It.IsAny<IPAddress>(), It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(ex);

                // Act.
                var result = await Assert.ThrowsAnyAsync<Exception>(
                    () => Subject.PostAsync(request, cancellationToken));

                // Assert.
                Assert.Same(ex, result);

                this.pool.Verify(p => p.TryLockAddressAsync(cancellationToken), Times.Once());
                this.Callbacks.Verify(r => r.AddAsync(requester, callback, CancellationToken.None), Times.Once());

                this.watcher.Verify(
                    w => w.StartWatchAsync(
                        It.IsAny<ReceivingAddressReservation>(),
                        It.IsAny<PropertyAmount>(),
                        It.IsAny<int>(),
                        It.IsAny<TimeSpan>(),
                        It.IsAny<TokenReceivingCallback>(),
                        It.IsAny<CancellationToken>()),
                        Times.Never());

                this.pool.Verify(p => p.ReleaseAddressAsync(this.reservation.Id, CancellationToken.None), Times.Once());
            });
        }

        [Fact]
        public Task PostAsync_ErrorWhileStartWatching_ShouldReleaseAddressReservation()
        {
            return AsynchronousTesting.WithCancellationTokenAsync(async cancellationToken =>
            {
                // Arrange.
                var ex = new Exception();

                var request = new ReceivingRequest()
                {
                    TargetAmount = new PropertyAmount(5),
                };

                this.pool
                    .Setup(p => p.TryLockAddressAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(this.reservation);

                this.watcher
                    .Setup(w => w.StartWatchAsync(
                        It.IsAny<ReceivingAddressReservation>(),
                        It.IsAny<PropertyAmount>(),
                        It.IsAny<int>(),
                        It.IsAny<TimeSpan>(),
                        It.IsAny<TokenReceivingCallback>(),
                        It.IsAny<CancellationToken>()))
                    .ThrowsAsync(ex);

                // Act.
                var result = await Assert.ThrowsAnyAsync<Exception>(
                    () => Subject.PostAsync(request, cancellationToken));

                // Assert.
                this.pool.Verify(p => p.TryLockAddressAsync(cancellationToken), Times.Once());

                this.watcher.Verify(
                    w => w.StartWatchAsync(
                        this.reservation,
                        new PropertyAmount(5),
                        6,
                        TimeSpan.FromHours(1),
                        null,
                        CancellationToken.None),
                    Times.Once());

                this.pool.Verify(p => p.ReleaseAddressAsync(this.reservation.Id, CancellationToken.None), Times.Once());
            });
        }

        protected override ReceivingController CreateController()
        {
            return new ReceivingController(this.config, this.pool.Object, this.watcher.Object, Helper);
        }
    }
}
