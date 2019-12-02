using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Ztm.Hosting
{
    public sealed class BackgroundServiceErrorLogger : BackgroundServiceExceptionHandler
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

        protected override Task RunAsync(Type service, Exception exception, CancellationToken cancellationToken)
        {
            this.logger.LogCritical(exception, "Fatal error occurred in {Service}.", service);

            return Task.CompletedTask;
        }
    }
}
