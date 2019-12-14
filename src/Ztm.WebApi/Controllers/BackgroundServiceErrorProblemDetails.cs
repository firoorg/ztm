using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Ztm.WebApi.Models;

namespace Ztm.WebApi.Controllers
{
    public sealed class BackgroundServiceErrorProblemDetails : ProblemDetails
    {
        public IEnumerable<BackgroundServiceError> Errors { get; set; }
    }
}