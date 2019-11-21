using System;
using System.Net;

namespace Ztm.WebApi
{
    public sealed class InvalidCallbackUrlException : ApiException
    {
        static readonly string StaticTitle = "Invalid Callback URL.";

        public InvalidCallbackUrlException()
            : base((int)HttpStatusCode.BadRequest, StaticTitle)
        {
        }

        public InvalidCallbackUrlException(string message)
            : base((int)HttpStatusCode.BadRequest, StaticTitle, message)
        {
        }

        public InvalidCallbackUrlException(string message, Exception inner)
            : base((int)HttpStatusCode.BadRequest, StaticTitle, message, inner)
        {
        }
    }
}