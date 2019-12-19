using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Ztm.ObjectModel;
using Ztm.Threading.TimerSchedulers;

namespace Ztm.Threading
{
    public sealed class Timer
    {
        readonly Stopwatch stopwatch;
        volatile TimerStatus status;
        volatile int oneShotState; // 0 = not one shot, 1 = not fired, 2 = stopping by outside, 3 = stopping by inside
        volatile ShutdownGuard stopGuard;
        volatile object schedule;
        volatile int elapsed;

        public Timer(ITimerScheduler scheduler = null)
        {
            this.stopwatch = new Stopwatch();

            Scheduler = scheduler ?? DefaultScheduler;
        }

        public static ITimerScheduler DefaultScheduler { get; } = new ThreadPoolScheduler();

        public int ElapsedCount => this.elapsed;

        public TimeSpan ElapsedTime => this.stopwatch.Elapsed;

        public ITimerScheduler Scheduler { get; }

        public TimerStatus Status => this.status;

        public event EventHandler<TimerElapsedEventArgs> Elapsed;

        public void Start(TimeSpan due, TimeSpan? period, object context)
        {
            if (!Scheduler.IsValidDuration(due))
            {
                throw new ArgumentOutOfRangeException(nameof(due), due, "The value is not valid.");
            }

            if (period != null && !Scheduler.IsValidDuration(period.Value))
            {
                throw new ArgumentOutOfRangeException(nameof(period), period, "The value is not valid.");
            }

            if (Status != TimerStatus.Created)
            {
                throw new InvalidOperationException("The timer is already started.");
            }

            // Prepare to schedule callback.
            this.stopGuard = new ShutdownGuard();
            this.oneShotState = (period != null) ? 0 : 1;
            this.status = TimerStatus.Started; // We need to change status here due to scheduling might elapsed immediately.

            // Schedule callback.
            try
            {
                this.stopwatch.Restart();
                this.schedule = Scheduler.Schedule(due, period, OnElapsed, context);
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
            Scheduler.Stop(this.schedule);
            this.stopwatch.Stop();
            this.stopGuard.Dispose();

            this.status = TimerStatus.Stopped;
        }

        async void OnElapsed(object context)
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
