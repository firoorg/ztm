using System.Threading;
using System.Threading.Tasks;

namespace Ztm.ServiceModel.Tests
{
    class TestBackgroundService : BackgroundService
    {
        public override string Name => "Test Background Service";

        public int OnStartAsyncCount { get; set; }

        public int OnStopAsyncCount { get; set; }

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
