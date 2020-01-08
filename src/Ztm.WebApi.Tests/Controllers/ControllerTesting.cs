using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ztm.WebApi.Tests.Controllers
{
    public static class ControllerTesting
    {
        public static void SetHttpContext(ControllerBase controller, Action<HttpContext> modifier = null)
        {
            var httpContext = new DefaultHttpContext();
            if (modifier != null)
            {
                modifier(httpContext);
            }

            if (controller.ControllerContext == null)
            {
                controller.ControllerContext = new ControllerContext();
            }

            controller.ControllerContext.HttpContext = httpContext;
        }
    }
}