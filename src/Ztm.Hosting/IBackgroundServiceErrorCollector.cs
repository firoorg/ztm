using System.Collections.Generic;

namespace Ztm.Hosting
{
    public interface IBackgroundServiceErrorCollector :
        IBackgroundServiceExceptionHandler,
        IEnumerable<BackgroundServiceError>
    {
    }
}
