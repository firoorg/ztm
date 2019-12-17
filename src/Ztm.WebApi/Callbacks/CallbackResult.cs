using System;

namespace Ztm.WebApi.Callbacks
{
    public class CallbackResult
    {
        public const string StatusError   = "error";
        public const string StatusSuccess = "success";
        public const string StatusUpdate  = "update";

        public CallbackResult(string status, object data)
        {
            if (status == null)
            {
                throw new ArgumentNullException(nameof(status));
            }

            this.Status = status;
            this.Data = data;
        }

        public string Status { get; }
        public object Data { get; }
    }
}