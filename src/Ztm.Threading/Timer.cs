using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Ztm.ObjectModel;

namespace Ztm.Threading
{
    public sealed class Timer
    {
        public static readonly TimeSpan MaxDuration = TimeSpan.FromMilliseconds(4294967294);
        public static readonly TimeSpan MinDuration = TimeSpan.Zero;

        readonly Stopwatch stopwatch;
        volatile TimerStatus status;
        volatile int oneShotState; // 0 = not one shot, 1 = not fired, 2 = stopping by outside, 3 = stopping by inside
        volatile ShutdownGuard stopGuard;
        volatile System.Threading.Timer timer;
        volatile int elapsed;

        public Timer()
        {
            this.stopwatch = new Stopwatch();
        }

        public int ElapsedCount => this.elapsed;

        public TimeSpan ElapsedTime => this.stopwatch.Elapsed;

        public TimerStatus Status => this.status;

        public event EventHandler<TimerElapsedEventArgs> Elapsed;

        public static bool IsValidDuration(TimeSpan duration)
        {
            return duration >= MinDuration && duration <= MaxDuration;
        }

        public void Start(TimeSpan due, TimeSpan? period, object context)
        {
            if (!IsValidDuration(due))
            {
                throw new ArgumentOutOfRangeException(nameof(due), due, "The value is not valid.");
            }

            if (period.HasValue && !IsValidDuration(period.Value))
            {
                throw new ArgumentOutOfRangeException(nameof(period), period, "The value is not valid");
            }

            if (Status != TimerStatus.Created)
            {
                throw new InvalidOperationException("The timer is already started.");
            }

            // Prepare to start timer.
            if (period.HasValue && period.Value == TimeSpan.Zero)
            {
                // If caller supply zero for period that mean they want to repeat timer every 0 seconds but
                // System.Threading.Timer treat that value as one-shot timer instead. So we want to emulate the behvaior
                // that caller expected.
                period = TimeSpan.FromMilliseconds(1);
            }

            this.stopGuard = new ShutdownGuard();
            this.oneShotState = period.HasValue ? 0 : 1;
            this.status = TimerStatus.Started; // We need to change status here due to timer might elapsed immediately.

            // Start timer.
            try
            {
                this.stopwatch.Restart();
                this.timer = new System.Threading.Timer(
                    TimerElapsedAsync,
                    context,
                    due,
                    period.HasValue ? period.Value : Timeout.InfiniteTimeSpan
                );
            }
            catch
            {
                this.stopwatch.Stop();
                this.status = TimerStatus.Created;
                this.oneShotState = 0;
                this.stopGuard.Dispose();
                this.stopGuard = null;
                throw;
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (Status != TimerStatus.Started)
            {
                throw new InvalidOperationException("The timer is not started.");
            }

            // Try to change one-shot state to 2 (stopping by outside) only if it is 1 (one-shot not fired). If the
            // current value is either 0 (not one-shot) or 1 that mean this method have responsibility to clean up
            // resources.
            switch (Interlocked.CompareExchange(ref this.oneShotState, 2, 1))
            {
                case 0: // Not one-shot.
                case 1: // One-shot and this method win for resource freeing.
                    await this.stopGuard.SetAndWaitAsync(cancellationToken);
                    Stop();
                    break;
            }
        }

        void Stop()
        {
            this.timer.Dispose();
            this.stopwatch.Stop();
            this.stopGuard.Dispose();

            this.status = TimerStatus.Stopped;
        }

        async void TimerElapsedAsync(object context)
        {
            if (!this.stopGuard.TryLock())
            {
                return;
            }

            try
            {
                await Elapsed.InvokeAsync(this, new TimerElapsedEventArgs(context, CancellationToken.None));
            }
            finally
            {
                Interlocked.Increment(ref this.elapsed);

                this.stopGuard.Release();

                // Try to change one-shot state to 3 (stopping by inside) only if it is 1 (one-shot not fired). If the
                // current value is 1 that mean this method have responsibility to clean up resources.
                if (Interlocked.CompareExchange(ref this.oneShotState, 3, 1) == 1)
                {
                    Stop();
                }
            }
        }
    }
}
