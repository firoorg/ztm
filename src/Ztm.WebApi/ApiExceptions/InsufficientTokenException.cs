using System;
using System.Net;

namespace Ztm.WebApi.ApiExceptions
{
    public sealed class InsufficientTokenException : ApiException
    {
        static readonly string StaticTitle = "Token is not enough";
        static readonly int StaticHttpStatusCode = (int)HttpStatusCode.Conflict;

        public InsufficientTokenException()
            : base(StaticHttpStatusCode, StaticTitle)
        {
        }

        public InsufficientTokenException(string message)
            : base(StaticHttpStatusCode, StaticTitle, message)
        {
        }

        public InsufficientTokenException(string message, Exception inner)
            : base(StaticHttpStatusCode, StaticTitle, message, inner)
        {
        }
    }
}