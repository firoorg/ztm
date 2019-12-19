using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;

namespace Ztm.Threading.Tests
{
    public sealed class TimerTests : IDisposable
    {
        readonly Mock<ITimerScheduler> scheduler;
        readonly Timer subject;

        public TimerTests()
        {
            this.scheduler = new Mock<ITimerScheduler>();
            this.subject = new Timer(this.scheduler.Object);
        }

        public void Dispose()
        {
            if (this.subject.Status == TimerStatus.Started)
            {
                this.subject.StopAsync(CancellationToken.None).Wait();
            }
        }

        [Fact]
        public void Constructor_WithNullScheduler_ShouldUseDefault()
        {
            var subject = new Timer(null);

            subject.Scheduler.Should().BeSameAs(Timer.DefaultScheduler);
        }

        [Fact]
        public void Constructor_WhenSucess_PropertiesShouldInitialized()
        {
            this.subject.ElapsedCount.Should().Be(0);
            this.subject.ElapsedTime.Should().Be(TimeSpan.Zero);
            this.subject.Scheduler.Should().BeSameAs(this.scheduler.Object);
            this.subject.Status.Should().Be(TimerStatus.Created);
        }

        [Fact]
        public void DefaultScheduler_WhenGetting_ShouldReturnNonNull()
        {
            Timer.DefaultScheduler.Should().NotBeNull();
        }

        [Fact]
        public void Start_WithInvalidDue_ShouldThrow()
        {
            this.scheduler.Setup(s => s.IsValidDuration(TimeSpan.MinValue))
                          .Returns(false);

            this.subject.Invoking(s => s.Start(TimeSpan.MinValue, null, null))
                        .Should().ThrowExactly<ArgumentOutOfRangeException>()
                        .And.ParamName.Should().Be("due");
        }

        [Fact]
        public void Start_WithInvalidPeriod_ShouldThrow()
        {
            this.scheduler.Setup(s => s.IsValidDuration(TimeSpan.MinValue))
                          .Returns(true);
            this.scheduler.Setup(s => s.IsValidDuration(TimeSpan.MaxValue))
                          .Returns(false);

            this.subject.Invoking(s => s.Start(TimeSpan.MinValue, TimeSpan.MaxValue, null))
                        .Should().ThrowExactly<ArgumentOutOfRangeException>()
                        .And.ParamName.Should().Be("period");
        }

        [Fact]
        public void Start_AlreadyStarted_ShouldThrow()
        {
            // Arrange.
            this.scheduler.Setup(s => s.IsValidDuration(TimeSpan.Zero))
                          .Returns(true);

            this.subject.Start(TimeSpan.Zero, TimeSpan.Zero, null);

            // Act.
            this.subject.Invoking(s => s.Start(TimeSpan.Zero, TimeSpan.Zero, null))
                        .Should().ThrowExactly<InvalidOperationException>();
        }

        [Fact]
        public async Task Start_AlreadyStopped_ShouldThrow()
        {
            // Arrange.
            this.scheduler.Setup(s => s.IsValidDuration(TimeSpan.Zero))
                          .Returns(true);

            this.subject.Start(TimeSpan.Zero, TimeSpan.Zero, null);
            await this.subject.StopAsync(CancellationToken.None);

            // Act.
            this.subject.Invoking(s => s.Start(TimeSpan.Zero, TimeSpan.Zero, null))
                        .Should().ThrowExactly<InvalidOperationException>();
        }

        [Theory]
        [InlineData(true, null)]
        [InlineData(true, "abc")]
        [InlineData(false, null)]
        [InlineData(false, "abc")]
        public void Start_WithValidArgs_ShouldScheduleHandler(bool oneshot, object context)
        {
            // Arrange.
            var period = oneshot ? (TimeSpan?)null : TimeSpan.Zero;

            this.scheduler.Setup(s => s.IsValidDuration(TimeSpan.Zero))
                          .Returns(true);

            // Act.
            this.subject.Start(TimeSpan.Zero, period, context);

            // Assert.
            this.scheduler.Verify(
                s => s.Schedule(TimeSpan.Zero, period, It.IsNotNull<Action<object>>(), context),
                Times.Once()
            );
        }

        [Fact]
        public void StopAsync_NotStarted_ShouldThrow()
        {
            this.subject.Invoking(s => s.StopAsync(CancellationToken.None))
                        .Should().ThrowExactly<InvalidOperationException>();
        }

        [Fact]
        public async Task StopAsync_AlreadyStopped_ShouldThrow()
        {
            // Arrange.
            this.scheduler.Setup(s => s.IsValidDuration(TimeSpan.Zero))
                          .Returns(true);

            this.subject.Start(TimeSpan.Zero, TimeSpan.Zero, null);
            await this.subject.StopAsync(CancellationToken.None);

            // Act.
            this.subject.Invoking(s => s.StopAsync(CancellationToken.None))
                        .Should().ThrowExactly<InvalidOperationException>();
        }

        [Fact]
        public async Task StopAsync_NotElapsed_ShouldStopScheduler()
        {
            // Arrange.
            var schedule = new object();
            Action trigger = null;
            var handler = new Mock<EventHandler<TimerElapsedEventArgs>>();

            this.scheduler.Setup(s => s.IsValidDuration(It.IsAny<TimeSpan>()))
                          .Returns(true);
            this.scheduler.Setup(s => s.Schedule(
                              It.IsAny<TimeSpan>(),
                              It.IsAny<TimeSpan?>(),
                              It.IsAny<Action<object>>(),
                              It.IsAny<object>()
                          ))
                          .Returns(schedule)
                          .Callback<TimeSpan, TimeSpan?, Action<object>, object>((d, p, h, c) => trigger = () => h(c));

            this.subject.Elapsed += handler.Object;
            this.subject.Start(TimeSpan.MaxValue, TimeSpan.Zero, null);

            // Act.
            await this.subject.StopAsync(CancellationToken.None);

            trigger(); // Emulate elapsed after stopped.

            // Assert.
            this.subject.ElapsedCount.Should().Be(0);
            this.subject.ElapsedTime.Should().BeGreaterThan(TimeSpan.Zero);
            this.subject.Status.Should().Be(TimerStatus.Stopped);

            this.scheduler.Verify(s => s.Stop(schedule), Times.Once());

            handler.Verify(h => h(It.IsAny<object>(), It.IsAny<TimerElapsedEventArgs>()), Times.Never());
        }

        [Fact]
        public async Task StopAsync_AlreadyElapsed_ShouldThrow()
        {
            // Arrange.
            var schedule = new object();
            Action trigger = null;
            var handler = new Mock<EventHandler<TimerElapsedEventArgs>>();
            var context = new object();

            this.scheduler.Setup(s => s.IsValidDuration(TimeSpan.Zero))
                          .Returns(true);
            this.scheduler.Setup(s => s.Schedule(It.IsAny<TimeSpan>(), It.IsAny<TimeSpan?>(), It.IsAny<Action<object>>(), It.IsAny<object>()))
                          .Returns(schedule)
                          .Callback<TimeSpan, TimeSpan?, Action<object>, object>((d, p, h, c) => trigger = () => h(c));

            this.subject.Elapsed += handler.Object;
            this.subject.Start(TimeSpan.Zero, null, context);

            // Act.
            trigger();

            await this.subject.Invoking(s => s.StopAsync(CancellationToken.None))
                              .Should().ThrowExactlyAsync<InvalidOperationException>();

            // Assert.
            this.subject.ElapsedCount.Should().Be(1);
            this.subject.ElapsedTime.Should().BeGreaterThan(TimeSpan.Zero);
            this.subject.Status.Should().Be(TimerStatus.Stopped);

            this.scheduler.Verify(s => s.Stop(schedule), Times.Once());

            handler.Verify(
                h => h(
                    this.subject,
                    It.Is<TimerElapsedEventArgs>(
                        e => e.Context == context && e.CancellationToken == CancellationToken.None
                    )
                ),
                Times.Once()
            );
        }
    }
}
