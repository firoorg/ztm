using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ztm.Hosting
{
    public abstract class BackgroundServiceExceptionHandler : IBackgroundServiceExceptionHandler
    {
        protected BackgroundServiceExceptionHandler()
        {
        }

        public Task InvokeRunAsync(Type service, Exception exception, CancellationToken cancellationToken)
        {
            return ((IBackgroundServiceExceptionHandler)this).RunAsync(service, exception, cancellationToken);
        }

        protected abstract Task RunAsync(Type service, Exception exception, CancellationToken cancellationToken);

        Task IBackgroundServiceExceptionHandler.RunAsync(
            Type service,
            Exception exception,
            CancellationToken cancellationToken)
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            return RunAsync(service, exception, cancellationToken);
        }
    }
}
