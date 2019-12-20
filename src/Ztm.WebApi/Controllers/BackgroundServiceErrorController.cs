using System;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Ztm.Hosting.AspNetCore;
using Ztm.WebApi.Models;

namespace Ztm.WebApi.Controllers
{
    [ApiController]
    public class BackgroundServiceErrorController : ControllerBase
    {
        readonly IHostingEnvironment hostingEnvironment;

        public BackgroundServiceErrorController(IHostingEnvironment hostingEnvironment)
        {
            if (hostingEnvironment == null)
            {
                throw new ArgumentNullException(nameof(hostingEnvironment));
            }

            this.hostingEnvironment = hostingEnvironment;
        }

        [Route("background-service-error")]
        public ActionResult BackgroundServiceError()
        {
            var feature = HttpContext.Features.Get<IBackgroundServiceExceptionHandlerFeature>();
            var details = new BackgroundServiceErrorProblemDetails()
            {
                Status = (int)HttpStatusCode.InternalServerError,
                Title = "The required background services was stopped unexpectedly."
            };

            if (feature != null && this.hostingEnvironment.IsDevelopment())
            {
                var errors = feature.Errors.Select(e => new BackgroundServiceError()
                {
                    Service = e.Service.FullName,
                    Error = e.Exception.Message,
                    Detail = e.Exception.StackTrace
                }).ToList();

                details.Errors = errors;
            }

            return StatusCode(details.Status.Value, details);
        }
    }
}
