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
        public ActionResult ErrorDevelopment()
        {
            var info = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            var ex = info?.Error;

            int status;
            string title;

            if (ex is ApiException apiEx)
            {
                status = apiEx.Status;
                title = $"{apiEx.Title} : {ex.GetType().Name}";
            }
            else
            {
                status = (int)HttpStatusCode.InternalServerError;
                title = ex.GetType().Name;
            }

            var problemDetails = new ProblemDetails
            {
                Status = status,
                Instance = info?.Path,
                Title = title,
                Detail = $"{ex.Message} : {ex.StackTrace}",
            };

            return StatusCode(problemDetails.Status.Value, problemDetails);
        }

        [Route("/error")]
        public ActionResult Error()
        {
            var info = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            var ex = info?.Error;

            int status;
            string title;

            if (ex is ApiException apiEx)
            {
                status = apiEx.Status;
                title = apiEx.Title;
            }
            else
            {
                status = (int)HttpStatusCode.InternalServerError;
                title = "An error occurred.";
            }

            var problemDetails = new ProblemDetails
            {
                Status = status,
                Instance = info?.Path,
                Title = title,
                Detail = null,
            };

            return StatusCode(problemDetails.Status.Value, problemDetails);
        }
    }
}