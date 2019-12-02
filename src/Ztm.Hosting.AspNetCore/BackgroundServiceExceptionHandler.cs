using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Ztm.Hosting.AspNetCore
{
    public class BackgroundServiceExceptionHandler : Ztm.Hosting.BackgroundServiceExceptionHandler
    {
        readonly Collection<IBackgroundServiceExceptionHandler> inners;

        public BackgroundServiceExceptionHandler(ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            Logger = new BackgroundServiceErrorLogger(loggerFactory.CreateLogger<BackgroundServiceErrorLogger>());
            Collector = new BackgroundServiceErrorCollector();

            this.inners = new Collection<IBackgroundServiceExceptionHandler>()
            {
                Logger,
                Collector
            };
        }

        public BackgroundServiceErrorCollector Collector { get; }

        public BackgroundServiceErrorLogger Logger { get; }

        protected override async Task RunAsync(Type service, Exception exception, CancellationToken cancellationToken)
        {
            // We cannot do IApplicationLifetime.StopApplication() here due to there is a race condition if background
            // task error too early. So we use another approach.
            foreach (var handler in this.inners)
            {
                await handler.RunAsync(service, exception, CancellationToken.None);
            }
        }
    }
}
