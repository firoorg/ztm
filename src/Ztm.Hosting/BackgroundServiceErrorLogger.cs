using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Ztm.Hosting
{
    public sealed class BackgroundServiceErrorLogger : IBackgroundServiceExceptionHandler
    {
        readonly ILogger logger;

        public BackgroundServiceErrorLogger(ILogger<BackgroundServiceErrorLogger> logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            this.logger = logger;
        }

        public Task RunAsync(Type service, Exception exception, CancellationToken cancellationToken)
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            this.logger.LogCritical(exception, "Fatal error occurred in {Service}.", service);

            return Task.CompletedTask;
        }
    }
}
