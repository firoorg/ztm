using System;
using System.Collections.ObjectModel;
using Xunit;
using Ztm.Testing;
using Ztm.Threading;
using Ztm.WebApi.AddressPools;
using Ztm.WebApi.Watchers.TokenReceiving;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.WebApi.Tests.Watchers.TokenReceiving
{
    public sealed class WatchingTests
    {
        readonly Rule rule;
        readonly Timer timer;
        readonly Watching subject;

        public WatchingTests()
        {
            var address = new ReceivingAddress(
                Guid.NewGuid(),
                TestAddress.Regtest1,
                true,
                new Collection<ReceivingAddressReservation>());
            var reservation = new ReceivingAddressReservation(Guid.NewGuid(), address, DateTime.Now, null);
            address.Reservations.Add(reservation);

            this.rule = new Rule(
                new PropertyId(3),
                reservation,
                new PropertyAmount(100),
                6,
                TimeSpan.FromHours(1),
                null);

            this.timer = new Timer();

            this.subject = new Watching(this.rule, this.timer);
        }

        [Fact]
        public void Constructor_WithNullRule_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>("rule", () => new Watching(null, this.timer));
        }

        [Fact]
        public void Constructor_WithNullTimer_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>("timer", () => new Watching(this.rule, null));
        }

        [Fact]
        public void Constructor_WhenSucceeded_ShouldInitializeProperties()
        {
            Assert.Equal(this.rule, this.subject.Rule);
            Assert.Equal(this.timer, this.subject.Timer);
        }

        [Fact]
        public void Deconstruct_ToRuleAndTimer_ShouldOutputRuleAndTimer()
        {
            var (rule, timer) = this.subject;

            Assert.Equal(this.subject.Rule, rule);
            Assert.Equal(this.subject.Timer, timer);
        }
    }
}
