using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ztm.Hosting
{
    public interface IBackgroundServiceExceptionHandler
    {
        Task RunAsync(Type service, Exception exception, CancellationToken cancellationToken);
    }
}
