using System;
using System.Net;

namespace Ztm.WebApi.ApiExceptions
{
    public class InputsChoosingException : ApiException
    {
        static readonly string StaticTitle = "Fail to choose inputs";
        static readonly int StaticHttpStatusCode = (int)HttpStatusCode.InternalServerError;

        public InputsChoosingException()
            : base(StaticHttpStatusCode, StaticTitle)
        {
        }

        public InputsChoosingException(string message)
            : base(StaticHttpStatusCode, StaticTitle, message)
        {
        }

        public InputsChoosingException(string message, Exception inner)
            : base(StaticHttpStatusCode, StaticTitle, message, inner)
        {
        }
    }
}