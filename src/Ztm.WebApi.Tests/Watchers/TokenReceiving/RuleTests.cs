using System;
using System.Collections.ObjectModel;
using System.Net;
using Xunit;
using Ztm.Testing;
using Ztm.WebApi.AddressPools;
using Ztm.WebApi.Callbacks;
using Ztm.WebApi.Watchers.TokenReceiving;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.WebApi.Tests.Watchers.TokenReceiving
{
    public sealed class RuleTests
    {
        readonly PropertyId property;
        readonly ReceivingAddress address;
        readonly ReceivingAddressReservation reservation;
        readonly PropertyAmount targetAmount;
        readonly int targetConfirmation;
        readonly TimeSpan timeout;
        readonly TokenReceivingCallback callback;
        readonly Guid id;
        readonly Rule subject;

        public RuleTests()
        {
            this.property = new PropertyId(3);
            this.address = new ReceivingAddress(
                Guid.NewGuid(),
                TestAddress.Regtest1,
                true,
                new Collection<ReceivingAddressReservation>());
            this.reservation = new ReceivingAddressReservation(Guid.NewGuid(), this.address, DateTime.Now, null);
            this.address.Reservations.Add(this.reservation);
            this.targetAmount = new PropertyAmount(100);
            this.targetConfirmation = 6;
            this.timeout = TimeSpan.FromHours(1);
            this.callback = new TokenReceivingCallback(
                new Callback(
                    Guid.NewGuid(),
                    IPAddress.Parse("192.168.1.2"),
                    DateTime.Now,
                    false,
                    new Uri("http://localhost")),
                "timeout");
            this.id = Guid.NewGuid();
            this.subject = new Rule(
                this.property,
                this.reservation,
                this.targetAmount,
                this.targetConfirmation,
                this.timeout,
                this.callback,
                this.id);
        }

        [Fact]
        public void Constructor_WithNullProperty_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "property",
                () => new Rule(
                    null,
                    this.reservation,
                    this.targetAmount,
                    this.targetConfirmation,
                    this.timeout,
                    this.callback));
            Assert.Throws<ArgumentNullException>(
                "property",
                () => new Rule(
                    null,
                    this.reservation,
                    this.targetAmount,
                    this.targetConfirmation,
                    this.timeout,
                    this.callback,
                    this.id));
        }

        [Fact]
        public void Constructor_WithNullAddressReservation_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "addressReservation",
                () => new Rule(
                    this.property,
                    null,
                    this.targetAmount,
                    this.targetConfirmation,
                    this.timeout,
                    this.callback));
            Assert.Throws<ArgumentNullException>(
                "addressReservation",
                () => new Rule(
                    this.property,
                    null,
                    this.targetAmount,
                    this.targetConfirmation,
                    this.timeout,
                    this.callback,
                    this.id));
        }

        [Fact]
        public void Constructor_WithNegativeTargetAmount_ShouldThrow()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                "targetAmount",
                () => new Rule(
                    this.property,
                    this.reservation,
                    PropertyAmount.MinusOne,
                    this.targetConfirmation,
                    this.timeout,
                    this.callback));
            Assert.Throws<ArgumentOutOfRangeException>(
                "targetAmount",
                () => new Rule(
                    this.property,
                    this.reservation,
                    PropertyAmount.MinusOne,
                    this.targetConfirmation,
                    this.timeout,
                    this.callback,
                    this.id));
        }

        [Fact]
        public void Constructor_WithZeroTargetAmount_ShouldThrow()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                "targetAmount",
                () => new Rule(
                    this.property,
                    this.reservation,
                    PropertyAmount.Zero,
                    this.targetConfirmation,
                    this.timeout,
                    this.callback));
            Assert.Throws<ArgumentOutOfRangeException>(
                "targetAmount",
                () => new Rule(
                    this.property,
                    this.reservation,
                    PropertyAmount.Zero,
                    this.targetConfirmation,
                    this.timeout,
                    this.callback,
                    this.id));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Constructor_WithInvalidTargetConfirmation_ShouldThrow(int confirmation)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                "targetConfirmation",
                () => new Rule(
                    this.property,
                    this.reservation,
                    this.targetAmount,
                    confirmation,
                    this.timeout,
                    this.callback));
            Assert.Throws<ArgumentOutOfRangeException>(
                "targetConfirmation",
                () => new Rule(
                    this.property,
                    this.reservation,
                    this.targetAmount,
                    confirmation,
                    this.timeout,
                    this.callback,
                    this.id));
        }

        [Fact]
        public void Constructor_WithInvalidOriginalTimeout_ShouldThrow()
        {
            var timeout = new TimeSpan(-1);

            Assert.Throws<ArgumentOutOfRangeException>(
                "originalTimeout",
                () => new Rule(
                    this.property,
                    this.reservation,
                    this.targetAmount,
                    this.targetConfirmation,
                    timeout,
                    this.callback));
            Assert.Throws<ArgumentOutOfRangeException>(
                "originalTimeout",
                () => new Rule(
                    this.property,
                    this.reservation,
                    this.targetAmount,
                    this.targetConfirmation,
                    timeout,
                    this.callback,
                    this.id));
        }

        [Fact]
        public void Constructor_WithNullCallback_ShouldNotThrow()
        {
            var rule1 = new Rule(
                this.property,
                this.reservation,
                this.targetAmount,
                this.targetConfirmation,
                this.timeout,
                null);

            var rule2 = new Rule(
                this.property,
                this.reservation,
                this.targetAmount,
                this.targetConfirmation,
                this.timeout,
                null,
                this.id);

            Assert.Null(rule1.Callback);
            Assert.Null(rule2.Callback);
        }

        [Fact]
        public void Constructor_WithNoId_IdShouldGenerated()
        {
            var subject = new Rule(
                this.property,
                this.reservation,
                this.targetAmount,
                this.targetConfirmation,
                this.timeout,
                this.callback);

            Assert.NotEqual(Guid.Empty, subject.Id);
        }

        [Fact]
        public void Constructor_WhenSucceeded_ShouldInitializeProperties()
        {
            Assert.Equal(this.reservation, this.subject.AddressReservation);
            Assert.Equal(this.callback, this.subject.Callback);
            Assert.Equal(this.id, this.subject.Id);
            Assert.Equal(this.timeout, this.subject.OriginalTimeout);
            Assert.Equal(this.property, this.subject.Property);
            Assert.Equal(this.targetAmount, this.subject.TargetAmount);
            Assert.Equal(this.targetConfirmation, this.subject.TargetConfirmation);
        }

        [Fact]
        public void Equals_WithEqual_ShouldReturnTrue()
        {
            var results = EqualityTesting.TestEquals(
                this.subject,
                s => new Rule(
                    new PropertyId(4),
                    s.AddressReservation,
                    s.TargetAmount,
                    s.TargetConfirmation,
                    s.OriginalTimeout,
                    s.Callback,
                    s.Id),
                s => new Rule(
                    s.Property,
                    new ReceivingAddressReservation(Guid.NewGuid(), this.address, DateTime.Now, null),
                    s.TargetAmount,
                    s.TargetConfirmation,
                    s.OriginalTimeout,
                    s.Callback,
                    s.Id),
                s => new Rule(
                    s.Property,
                    s.AddressReservation,
                    new PropertyAmount(50),
                    s.TargetConfirmation,
                    s.OriginalTimeout,
                    s.Callback,
                    s.Id),
                s => new Rule(
                    s.Property,
                    s.AddressReservation,
                    s.TargetAmount,
                    1,
                    s.OriginalTimeout,
                    s.Callback,
                    s.Id),
                s => new Rule(
                    s.Property,
                    s.AddressReservation,
                    s.TargetAmount,
                    s.TargetConfirmation,
                    TimeSpan.FromMinutes(30),
                    s.Callback,
                    s.Id),
                s => new Rule(
                    s.Property,
                    s.AddressReservation,
                    s.TargetAmount,
                    s.TargetConfirmation,
                    s.OriginalTimeout,
                    s.Callback,
                    s.Id),
                s => new Rule(
                    s.Property,
                    s.AddressReservation,
                    s.TargetAmount,
                    s.TargetConfirmation,
                    s.OriginalTimeout,
                    null,
                    s.Id));

            Assert.DoesNotContain(false, results);
        }

        [Fact]
        public void Equals_WithInequal_ShouldReturnFalse()
        {
            var results = EqualityTesting.TestInequal(
                this.subject,
                s => new Rule(
                    s.Property,
                    s.AddressReservation,
                    s.TargetAmount,
                    s.TargetConfirmation,
                    s.OriginalTimeout,
                    s.Callback,
                    Guid.NewGuid()));

            Assert.DoesNotContain(true, results);
        }
    }
}
