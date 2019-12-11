using System;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Xunit;

namespace Ztm.Threading.Tests
{
    public class TimerTests
    {
        readonly Timer subject;

        public TimerTests()
        {
            this.subject = new Timer();
        }

        [Fact]
        public void Constructor_WhenSucess_PropertiesShouldBeInitialized()
        {
            Assert.Equal(0, this.subject.ElapsedCount);
            Assert.Equal(TimeSpan.Zero, this.subject.ElapsedTime);
            Assert.Equal(TimerStatus.Created, this.subject.Status);
        }

        [Theory]
        [InlineData(-1d)]
        [InlineData(4294967295d)]
        public void IsValidDuration_WithInvalidValue_ShouldReturnFalse(double millis)
        {
            Assert.False(Timer.IsValidDuration(TimeSpan.FromMilliseconds(millis)));
        }

        [Theory]
        [InlineData(0d)]
        [InlineData(4294967294d)]
        public void IsValidDuration_WithValidValue_ShouldReturnTrue(double millis)
        {
            Assert.True(Timer.IsValidDuration(TimeSpan.FromMilliseconds(millis)));
        }

        [Theory]
        [InlineData(-1d)]
        [InlineData(4294967295d)]
        public void Start_WithInvalidDue_ShouldThrow(double millis)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                "due",
                () => this.subject.Start(TimeSpan.FromMilliseconds(millis), null, null)
            );
        }

        [Theory]
        [InlineData(-1d)]
        [InlineData(4294967295d)]
        public void Start_WithInvalidPeriod_ShouldThrow(double millis)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                "period",
                () => this.subject.Start(TimeSpan.Zero, TimeSpan.FromMilliseconds(millis), null)
            );
        }

        [Fact]
        public async Task Start_AlreadyStarted_ShouldThrow()
        {
            this.subject.Start(TimeSpan.Zero, TimeSpan.Zero, null);

            try
            {
                Assert.Throws<InvalidOperationException>(() => this.subject.Start(TimeSpan.Zero, TimeSpan.Zero, null));
            }
            finally
            {
                await this.subject.StopAsync(CancellationToken.None);
            }
        }

        [Fact]
        public async Task Start_AlreadyStopped_ShouldThrow()
        {
            this.subject.Start(TimeSpan.Zero, TimeSpan.Zero, null);
            await this.subject.StopAsync(CancellationToken.None);

            Assert.Throws<InvalidOperationException>(() => this.subject.Start(TimeSpan.Zero, TimeSpan.Zero, null));
        }

        [Fact]
        public async Task Start_WithNullPeriod_ShouldOneShot()
        {
            // Arrange.
            var handler = Substitute.For<EventHandler<TimerElapsedEventArgs>>();
            var context = new object();

            this.subject.Elapsed += handler;

            // Act.
            this.subject.Start(TimeSpan.Zero, null, context);
            await Task.Delay(500); // Ensure timer is fired.

            // Assert.
            Assert.Equal(TimerStatus.Stopped, this.subject.Status);
            Assert.Equal(1, this.subject.ElapsedCount);
            Assert.NotEqual(TimeSpan.Zero, this.subject.ElapsedTime);

            handler.Received(1)(this.subject, Arg.Is<TimerElapsedEventArgs>(e => e.Context == context));
        }

        [Fact]
        public async Task StopAsync_NotStarted_ShouldThrow()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => this.subject.StopAsync(CancellationToken.None));
        }

        [Fact]
        public async Task StopAsync_AlreadyStopped_ShouldThrow()
        {
            this.subject.Start(TimeSpan.Zero, TimeSpan.Zero, null);
            await this.subject.StopAsync(CancellationToken.None);

            await Assert.ThrowsAsync<InvalidOperationException>(() => this.subject.StopAsync(CancellationToken.None));
        }

        [Fact]
        public async Task StopAsync_WhenSucceeded_ShouldStopped()
        {
            // Arrange.
            var handler = Substitute.For<EventHandler<TimerElapsedEventArgs>>();
            var context = new object();

            this.subject.Elapsed += handler;
            this.subject.Start(TimeSpan.Zero, TimeSpan.Zero, context);

            await Task.Delay(2000);

            // Act.
            await this.subject.StopAsync(CancellationToken.None);

            await Task.Delay(1000); // Required to test if we handled internal timer queue correctly.

            // Assert.
            Assert.Equal(TimerStatus.Stopped, this.subject.Status);
            Assert.InRange(this.subject.ElapsedCount, 2, int.MaxValue);
            Assert.NotEqual(TimeSpan.Zero, this.subject.ElapsedTime);

            handler.Received(this.subject.ElapsedCount)(this.subject, Arg.Is<TimerElapsedEventArgs>(e => e.Context == context));
        }
    }
}
