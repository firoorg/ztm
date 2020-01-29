using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Ztm.Threading;

namespace Ztm.Testing
{
    public sealed class FakeTimerScheduler : ITimerScheduler
    {
        readonly Collection<ScheduleInfo> activeSchedules;
        readonly Collection<ScheduleInfo> stoppedSchedules;

        public FakeTimerScheduler()
        {
            this.activeSchedules = new Collection<ScheduleInfo>();
            this.stoppedSchedules = new Collection<ScheduleInfo>();
        }

        public IEnumerable<ScheduleInfo> ActiveSchedules => this.activeSchedules;

        public Func<TimeSpan, bool> DurationValidator { get; set; }

        public IEnumerable<ScheduleInfo> StoppedSchedules => this.stoppedSchedules;

        public bool IsValidDuration(TimeSpan duration)
        {
            return DurationValidator == null || DurationValidator(duration);
        }

        public object Schedule(TimeSpan due, TimeSpan? period, Action<object> handler, object context)
        {
            var info = new ScheduleInfo(due, period, handler, context);

            this.activeSchedules.Add(info);

            return info;
        }

        public void Stop(object schedule)
        {
            var info = (ScheduleInfo)schedule;

            this.activeSchedules.Remove(info);
            this.stoppedSchedules.Add(info);
        }

        public void Trigger(Func<ScheduleInfo, bool> criteria)
        {
            foreach (var schedule in this.activeSchedules.Where(criteria).ToList())
            {
                schedule.Handler(schedule.Context);
            }
        }

        public sealed class ScheduleInfo
        {
            public ScheduleInfo(TimeSpan due, TimeSpan? period, Action<object> handler, object context)
            {
                Due = due;
                Period = period;
                Handler = handler;
                Context = context;
                Id = Guid.NewGuid();
            }

            public object Context { get; }

            public TimeSpan Due { get; }

            public Action<object> Handler { get; }

            public TimeSpan? Period { get; }

            internal Guid Id { get; }

            public override bool Equals(object obj)
            {
                if (obj == null || obj.GetType() != GetType())
                {
                    return false;
                }

                return ((ScheduleInfo)obj).Id == Id;
            }

            public override int GetHashCode()
            {
                return Id.GetHashCode();
            }
        }
    }
}
