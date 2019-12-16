using System;

namespace Ztm.Threading
{
    public interface ITimerScheduler
    {
        bool IsValidDuration(TimeSpan duration);
        object Schedule(TimeSpan due, TimeSpan? period, Action<object> handler, object context);
        void Stop(object schedule);
    }
}
