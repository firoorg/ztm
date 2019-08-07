using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ztm.ServiceModel.Tests
{
    class TestBackgroundService : BackgroundService
    {
        public int OnStartAsyncCount { get; set; }

        public int OnStopAsyncCount { get; set; }

        public new void ScheduleStop(Exception exception)
        {
            base.ScheduleStop(exception);
        }

        protected override Task OnStartAsync(CancellationToken cancellationToken)
        {
            OnStartAsyncCount++;
            return Task.CompletedTask;
        }

        protected override Task OnStopAsync(CancellationToken cancellationToken)
        {
            OnStopAsyncCount++;
            return Task.CompletedTask;
        }
    }
}
