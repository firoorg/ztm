using System;

namespace Ztm.WebApi
{
    public abstract class HttpResponseException : Exception
    {
        public abstract int Status { get; }
        public abstract object Value { get; }
    }
}