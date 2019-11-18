using System;

namespace Ztm.WebApi
{
    public class HttpResponseException : Exception
    {
        public HttpResponseException(int status, string title)
        {
            this.Status = status;
            this.Title = title;
        }

        public HttpResponseException(int status, string title, string detail) : base(detail)
        {
            this.Status = status;
            this.Title = title;
        }

        public HttpResponseException(int status, string title, string detail, Exception ex) : base(detail, ex)
        {
            this.Status = status;
            this.Title = title;
        }

        public int Status { get; }
        public string Title { get; }
    }
}