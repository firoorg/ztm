using System.Collections.Generic;

namespace Ztm.Hosting.AspNetCore
{
    public interface IBackgroundServiceExceptionHandlerFeature
    {
        IEnumerable<BackgroundServiceError> Errors { get; }
    }
}
