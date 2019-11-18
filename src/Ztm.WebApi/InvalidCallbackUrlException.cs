using System;
using System.Net;

namespace Ztm.WebApi
{
    public sealed class InvalidCallbackUrlException : HttpResponseException
    {
        static readonly string InvalidCallbackUrlTitle = "Invalid Callback Url";

        public InvalidCallbackUrlException()
            : base((int)HttpStatusCode.BadRequest, InvalidCallbackUrlTitle)
        {
        }

        public InvalidCallbackUrlException(Exception ex)
            : base((int)HttpStatusCode.BadRequest, InvalidCallbackUrlTitle, null, ex)
        {
        }
    }
}