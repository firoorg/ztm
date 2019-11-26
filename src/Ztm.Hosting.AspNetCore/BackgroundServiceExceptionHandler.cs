using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Ztm.Hosting.AspNetCore
{
    public class BackgroundServiceExceptionHandler : IBackgroundServiceExceptionHandler
    {
        public BackgroundServiceExceptionHandler(ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            Logger = new BackgroundServiceErrorLogger(loggerFactory.CreateLogger<BackgroundServiceErrorLogger>());
            Collector = new BackgroundServiceErrorCollector();
        }

        public BackgroundServiceErrorCollector Collector { get; }

        public BackgroundServiceErrorLogger Logger { get; }

        public async Task RunAsync(Type service, Exception exception, CancellationToken cancellationToken)
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            // We cannot do IApplicationLifetime.StopApplication() here due to there is a race condition if background
            // task error too early. So we use another approach.
            await Logger.RunAsync(service, exception, cancellationToken);
            await Collector.RunAsync(service, exception, CancellationToken.None);
        }
    }
}
