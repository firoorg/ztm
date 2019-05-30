using System;
using System.Threading;
using System.Threading.Tasks;
using Ztm.ObjectModel;

namespace Ztm.ServiceModel
{
    public abstract class Service : IService
    {
        protected Service()
        {
        }

        public Exception Exception { get; protected set; }

        public bool IsRunning { get; private set; }

        public abstract string Name { get; }

        public event EventHandler<AsyncEventArgs> Stopped;

        public event EventHandler<AsyncEventArgs> Started;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        protected Task OnStoppedAsync(CancellationToken cancellationToken)
        {
            IsRunning = false;

            return Stopped.InvokeAsync(this, new AsyncEventArgs(cancellationToken));
        }

        protected Task OnStartedAsync(CancellationToken cancellationToken)
        {
            IsRunning = true;

            return Started.InvokeAsync(this, new AsyncEventArgs(cancellationToken));
        }
    }
}
