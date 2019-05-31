using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ztm.ServiceModel
{
    public abstract class BackgroundService : Service, IBackgroundService
    {
        readonly SemaphoreSlim stopAllowed;
        bool disposed;

        protected BackgroundService()
        {
            this.stopAllowed = new SemaphoreSlim(1, 1);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            if (IsRunning)
            {
                throw new InvalidOperationException("The service is already running.");
            }

            await OnStartAsync(cancellationToken);
            await OnStartedAsync(cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            await this.stopAllowed.WaitAsync(cancellationToken);

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
                this.stopAllowed.Release();
            }
        }

        protected void BeginStop()
        {
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

        protected override void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    if (IsRunning)
                    {
                        StopAsync(CancellationToken.None).Wait();
                    }

                    this.stopAllowed.Dispose();
                }

                this.disposed = true;
            }

            base.Dispose(disposing);
        }

        protected abstract Task OnStartAsync(CancellationToken cancellationToken);

        protected abstract Task OnStopAsync(CancellationToken cancellationToken);
    }
}
