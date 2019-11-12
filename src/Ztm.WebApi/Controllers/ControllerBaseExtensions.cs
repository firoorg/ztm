using System;
using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace Ztm.WebApi.Controllers
{
    public static class ControllerBaseExtensions
    {
        readonly static string CallbackUrlKey = "X-Callback-URL";
        readonly static string CallbackIdKey = "X-Callback-ID";

        public static bool TryGetCallbackUrl(this ControllerBase controller, out Uri url)
        {
            if (controller.HttpContext.Request.Headers.TryGetValue(CallbackUrlKey, out var rawUrl))
            {
                try
                {
                    url = new Uri(rawUrl);

                    if (url.Scheme == Uri.UriSchemeHttp
                        || url.Scheme == Uri.UriSchemeHttps)
                    {
                        return true;
                    }
                }
                catch (UriFormatException)
                {
                }
            }

            url = null;
            return false;
        }

        public static void SetCallbackId(this ControllerBase controller, Guid id)
        {
            controller.HttpContext.Response.Headers.Add(CallbackIdKey, id.ToString());
            controller.HttpContext.Response.StatusCode = (int)HttpStatusCode.Accepted;
        }
    }
}