using System;

namespace Ztm.Zcoin.Synchronization.Watchers
{
    public sealed class WatchToRemove<T> : IEquatable<WatchToRemove<T>> where T : Watch
    {
        public WatchToRemove(T watch, WatchRemoveReason reason)
        {
            if (watch == null)
            {
                throw new ArgumentNullException(nameof(watch));
            }

            Watch = watch;
            Reason = reason;
        }

        public WatchRemoveReason Reason { get; }

        public T Watch { get; }

        public bool Equals(WatchToRemove<T> other)
        {
            if (other == null || other.GetType() != GetType())
            {
                return false;
            }

            return other.Reason == Reason && other.Watch.Equals(Watch);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((WatchToRemove<T>)obj);
        }

        public override int GetHashCode()
        {
            return Watch.GetHashCode();
        }
    }
}
