using System.Net;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace Ztm.WebApi.Controllers
{
    [ApiController]
    public class ErrorController : ControllerBase
    {
        [Route("/error-development")]
        public ActionResult ErrorDevelopment([FromServices] IHostingEnvironment webHostEnvironment)
        {
            var feature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            var ex = feature?.Error;
            var isHttpResponseException = ex is HttpResponseException;

            var problemDetails = new ProblemDetails
            {
                Status = isHttpResponseException ? ((HttpResponseException)ex).Status : (int)HttpStatusCode.InternalServerError,
                Instance = feature?.Path,
                Title = isHttpResponseException ? $"{((HttpResponseException)ex).Title} : {ex.GetType().Name}" : ex.GetType().Name,
                Detail = $"{ex.Message} : {ex.StackTrace}",
            };

            return StatusCode(problemDetails.Status.Value, problemDetails);
        }

        [Route("/error")]
        public ActionResult Error([FromServices] IHostingEnvironment webHostEnvironment)
        {
            var feature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            var ex = feature?.Error;
            var isHttpResponseException = ex is HttpResponseException;

            var problemDetails = new ProblemDetails
            {
                Status = isHttpResponseException ? ((HttpResponseException)ex).Status : (int)HttpStatusCode.InternalServerError,
                Instance = feature?.Path,
                Title = isHttpResponseException ? ((HttpResponseException)ex).Title : "An error occurred.",
                Detail = null,
            };

            return StatusCode(problemDetails.Status.Value, problemDetails);
        }
    }
}