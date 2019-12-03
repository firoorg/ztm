using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Ztm.Hosting
{
    public abstract class BackgroundService : IDisposable, IHostedService
    {
        readonly IBackgroundServiceExceptionHandler exceptionHandler;
        readonly CancellationTokenSource cancellation;
        Task background;
        bool disposed;

        protected BackgroundService(IBackgroundServiceExceptionHandler exceptionHandler)
        {
            if (exceptionHandler == null)
            {
                throw new ArgumentNullException(nameof(exceptionHandler));
            }

            this.exceptionHandler = exceptionHandler;
            this.cancellation = new CancellationTokenSource();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (this.background != null)
            {
                throw new InvalidOperationException("The service is already started.");
            }

            this.background = RunBackgroundTaskAsync(this.cancellation.Token);

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (this.background == null)
            {
                throw new InvalidOperationException("The service was not started.");
            }

            this.cancellation.Cancel();

            try
            {
                // This method must ignore any errors that was raised from background task due to it is already handled
                // by exception handler. But we still need to throw OperationCanceledException if cancellationToken is
                // triggered.
                var completed = await Task.WhenAny(
                    this.background,
                    Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken)
                );

                if (!ReferenceEquals(completed, this.background))
                {
                    await completed;
                }
            }
            finally
            {
                this.background = null;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            if (disposing)
            {
                if (this.background != null)
                {
                    StopAsync(CancellationToken.None).Wait();
                }

                this.cancellation.Dispose();
            }

            this.disposed = true;
        }

        protected abstract Task ExecuteAsync(CancellationToken cancellationToken);

        async Task RunBackgroundTaskAsync(CancellationToken cancellationToken)
        {
            await Task.Yield(); // We don't want the code after this to run synchronously.

            try
            {
                await ExecuteAsync(cancellationToken);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                await this.exceptionHandler.RunAsync(GetType(), ex, CancellationToken.None);
            }
        }
    }
}
