using System.Net;

namespace Ztm.WebApi
{
    public sealed class InvalidCallbackUrlException : HttpResponseException
    {
        public InvalidCallbackUrlException(string InvalidUrl)
        {
            this.InvalidUrl = InvalidUrl;
        }

        public override int Status => (int)HttpStatusCode.BadRequest;

        public override object Value
        {
            get
            {
                return $"Callback Url `{this.InvalidUrl}` is invalid";
            }
        }

        readonly string InvalidUrl;
    }
}