using System.Collections.Generic;

namespace Ztm.ServiceModel
{
    public interface IServiceManager : IBackgroundService
    {
        IEnumerable<IService> Services { get; }

        void Add(IService service);

        void Remove(IService service);
    }
}
