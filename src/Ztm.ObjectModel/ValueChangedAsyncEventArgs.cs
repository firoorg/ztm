using System.Threading;

namespace Ztm.ObjectModel
{
    public class ValueChangedAsyncEventArgs<T> : AsyncEventArgs
    {
        public ValueChangedAsyncEventArgs(T current, T previous, CancellationToken cancellationToken)
            : base(cancellationToken)
        {
            Current = current;
            Previous = previous;
        }

        public T Current { get; }

        public T Previous { get; }
    }
}
