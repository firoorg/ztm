using System;
using System.Threading;

namespace Ztm.Threading.TimerSchedulers
{
    public sealed class ThreadPoolScheduler : ITimerScheduler
    {
        /// <summary>
        /// Maximum duration that supported by this scheduler.
        /// </summary>
        /// <remarks>
        /// This is the maximum value that supported by <see cref="System.Threading.Timer"/>.
        /// </remarks>
        public static readonly TimeSpan MaxDuration = TimeSpan.FromMilliseconds(4294967294);

        /// <summary>
        /// Minimum duration that supported by this scheduler.
        /// </summary>
        /// <remarks>
        /// This is the minimum value that supported by <see cref="System.Threading.Timer"/>.
        /// </remarks>
        public static readonly TimeSpan MinDuration = TimeSpan.Zero;

        public bool IsValidDuration(TimeSpan duration)
        {
            return duration >= MinDuration && duration <= MaxDuration;
        }

        public object Schedule(TimeSpan due, TimeSpan? period, Action<object> handler, object context)
        {
            if (!IsValidDuration(due))
            {
                throw new ArgumentOutOfRangeException(nameof(due), due, "The value is not valid duration.");
            }

            if (period != null && !IsValidDuration(period.Value))
            {
                throw new ArgumentOutOfRangeException(nameof(period), period, "The value is not valid duration.");
            }

            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            if (period != null && period.Value == TimeSpan.Zero)
            {
                // If caller supply zero for period that mean they want to repeat timer every 0 seconds but
                // System.Threading.Timer treat that value as one-shot timer. So we want to emulate the behvaior that
                // caller expected.
                period = TimeSpan.FromMilliseconds(1);
            }

            return new System.Threading.Timer(
                c => handler(c),
                context,
                due,
                (period != null) ? period.Value : Timeout.InfiniteTimeSpan
            );
        }

        public void Stop(object schedule)
        {
            if (schedule == null)
            {
                throw new ArgumentNullException(nameof(schedule));
            }

            if (schedule.GetType() != typeof(System.Threading.Timer))
            {
                throw new ArgumentException($"The object is not {typeof(System.Threading.Timer)}.", nameof(schedule));
            }

            ((System.Threading.Timer)schedule).Dispose();
        }
    }
}
