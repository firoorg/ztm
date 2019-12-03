using System;
using System.Collections.Generic;

namespace Ztm.Hosting.AspNetCore
{
    public class BackgroundServiceExceptionHandlerFeature : IBackgroundServiceExceptionHandlerFeature
    {
        public BackgroundServiceExceptionHandlerFeature(IEnumerable<BackgroundServiceError> errors)
        {
            if (errors == null)
            {
                throw new ArgumentNullException(nameof(errors));
            }

            Errors = errors;
        }

        public IEnumerable<BackgroundServiceError> Errors { get; }
    }
}
