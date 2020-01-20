using System;
using Xunit;
using Ztm.Testing;
using Ztm.WebApi.Watchers.TokenBalance;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.WebApi.Tests.Watchers.TokenBalance
{
    public sealed class RuleTests
    {
        readonly PropertyId property;
        readonly PropertyAmount targetAmount;
        readonly int targetConfirmation;
        readonly TimeSpan timeout;
        readonly string timeoutStatus;
        readonly Guid callback;
        readonly Guid id;
        readonly Rule subject;

        public RuleTests()
        {
            this.property = new PropertyId(3);
            this.targetAmount = new PropertyAmount(100);
            this.targetConfirmation = 6;
            this.timeout = TimeSpan.FromHours(1);
            this.timeoutStatus = "timeout";
            this.callback = Guid.NewGuid();
            this.id = Guid.NewGuid();
            this.subject = new Rule(
                this.property,
                TestAddress.Regtest1,
                this.targetAmount,
                this.targetConfirmation,
                this.timeout,
                this.timeoutStatus,
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
                    TestAddress.Regtest1,
                    this.targetAmount,
                    this.targetConfirmation,
                    this.timeout,
                    this.timeoutStatus,
                    this.callback));
            Assert.Throws<ArgumentNullException>(
                "property",
                () => new Rule(
                    null,
                    TestAddress.Regtest1,
                    this.targetAmount,
                    this.targetConfirmation,
                    this.timeout,
                    this.timeoutStatus,
                    this.callback,
                    this.id));
        }

        [Fact]
        public void Constructor_WithNullAddress_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "address",
                () => new Rule(
                    this.property,
                    null,
                    this.targetAmount,
                    this.targetConfirmation,
                    this.timeout,
                    this.timeoutStatus,
                    this.callback));
            Assert.Throws<ArgumentNullException>(
                "address",
                () => new Rule(
                    this.property,
                    null,
                    this.targetAmount,
                    this.targetConfirmation,
                    this.timeout,
                    this.timeoutStatus,
                    this.callback,
                    this.id));
        }

        [Fact]
        public void Constructor_WithNegativeTargetAmount_ShouldThrow()
        {
            var amount = new PropertyAmount(-1);

            Assert.Throws<ArgumentOutOfRangeException>(
                "targetAmount",
                () => new Rule(
                    this.property,
                    TestAddress.Regtest1,
                    amount,
                    this.targetConfirmation,
                    this.timeout,
                    this.timeoutStatus,
                    this.callback));
            Assert.Throws<ArgumentOutOfRangeException>(
                "targetAmount",
                () => new Rule(
                    this.property,
                    TestAddress.Regtest1,
                    amount,
                    this.targetConfirmation,
                    this.timeout,
                    this.timeoutStatus,
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
                    TestAddress.Regtest1,
                    this.targetAmount,
                    confirmation,
                    this.timeout,
                    this.timeoutStatus,
                    this.callback));
            Assert.Throws<ArgumentOutOfRangeException>(
                "targetConfirmation",
                () => new Rule(
                    this.property,
                    TestAddress.Regtest1,
                    this.targetAmount,
                    confirmation,
                    this.timeout,
                    this.timeoutStatus,
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
                    TestAddress.Regtest1,
                    this.targetAmount,
                    this.targetConfirmation,
                    timeout,
                    this.timeoutStatus,
                    this.callback));
            Assert.Throws<ArgumentOutOfRangeException>(
                "originalTimeout",
                () => new Rule(
                    this.property,
                    TestAddress.Regtest1,
                    this.targetAmount,
                    this.targetConfirmation,
                    timeout,
                    this.timeoutStatus,
                    this.callback,
                    this.id));
        }

        [Fact]
        public void Constructor_WithNullTimeoutStatus_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "timeoutStatus",
                () => new Rule(
                    this.property,
                    TestAddress.Regtest1,
                    this.targetAmount,
                    this.targetConfirmation,
                    this.timeout,
                    null,
                    this.callback));
            Assert.Throws<ArgumentNullException>(
                "timeoutStatus",
                () => new Rule(
                    this.property,
                    TestAddress.Regtest1,
                    this.targetAmount,
                    this.targetConfirmation,
                    this.timeout,
                    null,
                    this.callback,
                    this.id));
        }

        [Fact]
        public void Constructor_WithNoId_IdShouldGenerated()
        {
            var subject = new Rule(
                this.property,
                TestAddress.Regtest1,
                this.targetAmount,
                this.targetConfirmation,
                this.timeout,
                this.timeoutStatus,
                this.callback);

            Assert.NotEqual(Guid.Empty, subject.Id);
        }

        [Fact]
        public void Constructor_WhenSucceeded_ShouldInitializeProperties()
        {
            Assert.Equal(TestAddress.Regtest1, this.subject.Address);
            Assert.Equal(this.callback, this.subject.Callback);
            Assert.Equal(this.id, this.subject.Id);
            Assert.Equal(this.timeout, this.subject.OriginalTimeout);
            Assert.Equal(this.property, this.subject.Property);
            Assert.Equal(this.targetAmount, this.subject.TargetAmount);
            Assert.Equal(this.targetConfirmation, this.subject.TargetConfirmation);
            Assert.Equal(this.timeoutStatus, this.subject.TimeoutStatus);
        }

        [Fact]
        public void Equals_WithEqual_ShouldReturnTrue()
        {
            var results = EqualityTesting.TestEquals(
                this.subject,
                s => new Rule(
                    new PropertyId(4),
                    s.Address,
                    s.TargetAmount,
                    s.TargetConfirmation,
                    s.OriginalTimeout,
                    s.TimeoutStatus,
                    s.Callback,
                    s.Id),
                s => new Rule(
                    s.Property,
                    TestAddress.Regtest2,
                    s.TargetAmount,
                    s.TargetConfirmation,
                    s.OriginalTimeout,
                    s.TimeoutStatus,
                    s.Callback,
                    s.Id),
                s => new Rule(
                    s.Property,
                    s.Address,
                    new PropertyAmount(50),
                    s.TargetConfirmation,
                    s.OriginalTimeout,
                    s.TimeoutStatus,
                    s.Callback,
                    s.Id),
                s => new Rule(
                    s.Property,
                    s.Address,
                    s.TargetAmount,
                    1,
                    s.OriginalTimeout,
                    s.TimeoutStatus,
                    s.Callback,
                    s.Id),
                s => new Rule(
                    s.Property,
                    s.Address,
                    s.TargetAmount,
                    s.TargetConfirmation,
                    TimeSpan.FromMinutes(30),
                    s.TimeoutStatus,
                    s.Callback,
                    s.Id),
                s => new Rule(
                    s.Property,
                    s.Address,
                    s.TargetAmount,
                    s.TargetConfirmation,
                    s.OriginalTimeout,
                    "timedout",
                    s.Callback,
                    s.Id),
                s => new Rule(
                    s.Property,
                    s.Address,
                    s.TargetAmount,
                    s.TargetConfirmation,
                    s.OriginalTimeout,
                    s.TimeoutStatus,
                    Guid.NewGuid(),
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
                    s.Address,
                    s.TargetAmount,
                    s.TargetConfirmation,
                    s.OriginalTimeout,
                    s.TimeoutStatus,
                    s.Callback,
                    Guid.NewGuid()));

            Assert.DoesNotContain(true, results);
        }
    }
}
