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

        public override bool Equals(object other)
        {
            if (other == null || other.GetType() != GetType())
            {
                return false;
            }

            var otherResult = (CallbackResult)other;
            return Status.Equals(otherResult.Status) && Data.Equals(otherResult.Data);
        }

        public override int GetHashCode()
        {
            int h = 0;

            h ^= Status.GetHashCode();

            if (Data != null)
            {
                h ^= Data.GetHashCode();
            }

            return h;
        }
    }
}