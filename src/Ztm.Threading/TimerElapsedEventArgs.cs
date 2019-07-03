using System.Threading;
using Ztm.ObjectModel;

namespace Ztm.Threading
{
    public class TimerElapsedEventArgs : AsyncEventArgs
    {
        public TimerElapsedEventArgs(object context, CancellationToken cancellationToken) : base(cancellationToken)
        {
            Context = context;
        }

        public object Context { get; }
    }
}
