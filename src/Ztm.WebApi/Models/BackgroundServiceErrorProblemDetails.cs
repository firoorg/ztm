using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace Ztm.WebApi.Models
{
    public sealed class BackgroundServiceErrorProblemDetails : ProblemDetails
    {
        public IEnumerable<BackgroundServiceError> Errors { get; set; }
    }
}