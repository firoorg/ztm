using Microsoft.Extensions.Hosting;

namespace Ztm.ServiceModel
{
    public interface IBackgroundService : IHostedService, IService
    {
    }
}
