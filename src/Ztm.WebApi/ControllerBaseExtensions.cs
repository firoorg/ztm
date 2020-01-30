using System;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Ztm.WebApi.ApiExceptions;
using Ztm.WebApi.Callbacks;

namespace Ztm.WebApi
{
    public static class ControllerBaseExtensions
    {
        static readonly string CallbackUrlHeader = "X-Callback-URL";
        static readonly string CallbackIdHeader = "X-Callback-ID";

        public static AcceptedResult AcceptedWithCallback(this ControllerBase controller, Callback callback)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            controller.SetCallbackId(callback.Id);
            return controller.Accepted();
        }

        public static ObjectResult InsufficientFee(this ControllerBase controller)
        {
            return controller.StatusCode(
                (int)HttpStatusCode.InternalServerError,
                new { Message = "Fee is not enough" });
        }

        public static ConflictObjectResult InsufficientToken(this ControllerBase controller)
        {
            return controller.Conflict(new { Message = "Token is not enough" });
        }

        public static void SetCallbackId(this ControllerBase controller, Guid id)
        {
            controller.Response.Headers.Add(CallbackIdHeader, id.ToString());
        }

        public static bool TryGetCallbackUrl(this ControllerBase controller, out Uri url)
        {
            if (controller.Request.Headers.TryGetValue(CallbackUrlHeader, out var rawUrl))
            {
                try
                {
                    url = new Uri(rawUrl, UriKind.Absolute);
                }
                catch (UriFormatException ex)
                {
                    throw new InvalidCallbackUrlException($"`{rawUrl}` is invalid URL.", ex);
                }

                if (url.Scheme == Uri.UriSchemeHttp || url.Scheme == Uri.UriSchemeHttps)
                {
                    return true;
                }

                throw new InvalidCallbackUrlException();
            }

            url = null;
            return false;
        }
    }
}
