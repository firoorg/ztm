using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ztm.ObjectModel;

namespace Ztm.ServiceModel
{
    public class ServiceManager : BackgroundService, IServiceManager
    {
        readonly List<IService> services;

        public ServiceManager()
        {
            this.services = new List<IService>();
        }

        public ServiceManager(IEnumerable<IService> services) : this()
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            try
            {
                foreach (var service in services)
                {
                    Add(service);
                }
            }
            catch
            {
                foreach (var service in this.services)
                {
                    service.Stopped -= OnServiceStopped;
                }
                throw;
            }
        }

        public override string Name => "Service Manager";

        public IEnumerable<IService> Services => this.services;

        public void Add(IService service)
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            if (service.IsRunning)
            {
                throw new ArgumentException("The service is already running.", nameof(service));
            }

            if (this.services.Contains(service, ReferenceEqualityComparer<IService>.Default))
            {
                throw new ArgumentException("The service is already added.", nameof(service));
            }

            if (IsRunning)
            {
                throw new InvalidOperationException("The service manager is already running.");
            }

            service.Stopped += OnServiceStopped;

            this.services.Add(service);
        }

        public void Remove(IService service)
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            var index = this.services.FindIndex(s => ReferenceEqualityComparer<IService>.Default.Equals(s, service));

            if (index == -1)
            {
                throw new ArgumentException("The service was not added.", nameof(service));
            }

            if (IsRunning)
            {
                throw new InvalidOperationException("The service manager is already running.");
            }

            service.Stopped -= OnServiceStopped;

            this.services.RemoveAt(index);
        }

        protected override async Task OnStartAsync(CancellationToken cancellationToken)
        {
            var started = new List<IService>();

            try
            {
                foreach (var service in this.services)
                {
                    switch (service)
                    {
                        case IBackgroundService s:
                            await s.StartAsync(cancellationToken);
                            break;
                        default:
                            throw new InvalidOperationException($"Service {service.GetType().FullName} is not supported.");
                    }

                    started.Add(service);
                }
            }
            catch
            {
                // Stop all started service.
                started.Reverse();

                foreach (var service in started)
                {
                    try
                    {
                        switch (service)
                        {
                            case IBackgroundService s:
                                await s.StopAsync(CancellationToken.None);
                                break;
                            default:
                                Debug.Fail($"Don't know how to stop {service.GetType().FullName}.");
                                break;
                        }
                    }
                    catch
                    {
                        // Ignore.
                    }
                }
                throw;
            }
        }

        protected override async Task OnStopAsync(CancellationToken cancellationToken)
        {
            foreach (var service in this.services.Reverse<IService>())
            {
                switch (service)
                {
                    case IBackgroundService s:
                        try
                        {
                            await s.StopAsync(cancellationToken);
                        }
                        catch (InvalidOperationException)
                        {
                            // The service is already stopped, so ignore it.
                        }
                        break;
                    default:
                        Debug.Fail($"Don't know how to stop {service.GetType().FullName}.");
                        break;
                }
            }

            Debug.Assert(!this.services.Any(s => s.IsRunning));
        }

        void OnServiceStopped(object sender, AsyncEventArgs e)
        {
            var service = (IService)sender;

            if (service.Exception != null)
            {
                // If there is a faulted service we need to stop now.
                TrySetException(service.Exception);
            }
            else if (service.Exception == null && this.services.Any(s => s.IsRunning))
            {
                // There is services that still running so don't stop manager.
                return;
            }

            ScheduleStop(null);
        }
    }
}
