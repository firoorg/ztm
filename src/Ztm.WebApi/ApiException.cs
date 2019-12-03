using System;

namespace Ztm.WebApi
{
    public class ApiException : Exception
    {
        public ApiException(int status, string title)
        {
            if (title == null)
            {
                throw new ArgumentNullException(nameof(title));
            }

            Status = status;
            Title = title;
        }

        public ApiException(int status, string title, string message) : base(message)
        {
            if (title == null)
            {
                throw new ArgumentNullException(nameof(title));
            }

            Status = status;
            Title = title;
        }

        public ApiException(int status, string title, string message, Exception inner) : base(message, inner)
        {
            if (title == null)
            {
                throw new ArgumentNullException(nameof(title));
            }

            Status = status;
            Title = title;
        }

        public int Status { get; }
        public string Title { get; }
    }
}