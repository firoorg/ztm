using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ztm.ObjectModel;
using Ztm.Zcoin.Synchronization;

namespace Ztm.WebApi
{
    sealed class ServiceManager : IHostedService, IDisposable
    {
        readonly IApplicationLifetime applicationLifetime;
        readonly ILogger logger;
        readonly Ztm.ServiceModel.ServiceManager services;
        bool disposed;

        public ServiceManager(
            IApplicationLifetime applicationLifetime,
            IServiceProvider services,
            ILogger<ServiceManager> logger)
        {
            if (applicationLifetime == null)
            {
                throw new ArgumentNullException(nameof(applicationLifetime));
            }

            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            this.applicationLifetime = applicationLifetime;
            this.logger = logger;
            this.services = new Ztm.ServiceModel.ServiceManager();

            try
            {
                this.services.Stopped += OnStopped;
                this.services.Add(services.GetRequiredService<IBlocksSynchronizer>());
            }
            catch
            {
                this.services.Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.services.Dispose();

            this.disposed = true;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return this.services.StartAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            if (!this.services.IsRunning)
            {
                return Task.CompletedTask;
            }

            return this.services.StopAsync(cancellationToken);
        }

        void OnStopped(object sender, AsyncEventArgs e)
        {
            var manager = (Ztm.ServiceModel.IServiceManager)sender;

            if (manager.Exception != null)
            {
                this.logger.LogCritical(manager.Exception, "Background services stopped unexpectedly");
            }

            applicationLifetime.StopApplication();
        }
    }
}
