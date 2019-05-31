using System.Threading;
using System.Threading.Tasks;

namespace Ztm.ServiceModel
{
    public interface IBackgroundService : IService
    {
        Task StartAsync(CancellationToken cancellationToken);

        Task StopAsync(CancellationToken cancellationToken);
    }
}
