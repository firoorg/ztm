using System.Threading;
using System.Threading.Tasks;

namespace Ztm.ServiceModel.Tests
{
    class TestService : Service
    {
        public override string Name => "Test Service";

        public new Task OnStoppedAsync(CancellationToken cancellationToken)
        {
            return base.OnStoppedAsync(cancellationToken);
        }

        public new Task OnStartedAsync(CancellationToken cancellationToken)
        {
            return base.OnStartedAsync(cancellationToken);
        }
    }
}
