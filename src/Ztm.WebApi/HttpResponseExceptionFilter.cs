using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;

namespace Ztm.WebApi
{
    class ErrorDetails
    {
        public object Message { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class HttpResponseExceptionFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.Exception is HttpResponseException exception)
            {
                context.Result = new JsonResult(new ErrorDetails{Message = exception.Value})
                {
                    StatusCode = exception.Status,
                };

                context.ExceptionHandled = true;
            }
        }
    }
}