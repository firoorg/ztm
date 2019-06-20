using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ztm.ServiceModel
{
    public abstract class BackgroundService : Service, IBackgroundService
    {
        readonly SemaphoreSlim semaphore;
        bool disposed;

        protected BackgroundService()
        {
            this.semaphore = new SemaphoreSlim(1, 1);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            await this.semaphore.WaitAsync(cancellationToken);

            try
            {
                if (IsRunning)
                {
                    throw new InvalidOperationException("The service is already running.");
                }

                await OnStartAsync(cancellationToken);
                await OnStartedAsync(cancellationToken);
            }
            finally
            {
                this.semaphore.Release();
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            await this.semaphore.WaitAsync(cancellationToken);

            try
            {
                if (!IsRunning)
                {
                    throw new InvalidOperationException("The service is already stopped.");
                }

                await OnStopAsync(cancellationToken);
                await OnStoppedAsync(cancellationToken);
            }
            finally
            {
                this.semaphore.Release();
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!this.disposed)
            {
                if (disposing)
                {
                    if (IsRunning)
                    {
                        StopAsync(CancellationToken.None).Wait();
                    }

                    this.semaphore.Dispose();
                }

                this.disposed = true;
            }
        }

        protected abstract Task OnStartAsync(CancellationToken cancellationToken);

        protected abstract Task OnStopAsync(CancellationToken cancellationToken);

        protected void ScheduleStop(Exception exception)
        {
            if (exception != null)
            {
                TrySetException(exception);
            }

            Task.Run(async () =>
            {
                try
                {
                    await StopAsync(CancellationToken.None);
                }
                catch (ObjectDisposedException)
                {
                    // Ignore.
                }
                catch (InvalidOperationException)
                {
                    // Ignore.
                }
            });
        }
    }
}
