using System;

namespace Ztm.Hosting
{
    public sealed class BackgroundServiceError
    {
        public BackgroundServiceError(Type service, Exception exception)
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            Service = service;
            Exception = exception;
        }

        public Exception Exception { get; }

        public Type Service { get; }
    }
}
