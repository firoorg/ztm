using System;

namespace Ztm.ObjectModel
{
    public class ValueChangedEventArgs<T> : EventArgs
    {
        public ValueChangedEventArgs(T current, T previous)
        {
            Current = current;
            Previous = previous;
        }

        public T Current { get; }

        public T Previous { get; }
    }
}
