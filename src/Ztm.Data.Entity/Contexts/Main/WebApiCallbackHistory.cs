using System;

namespace Ztm.Data.Entity.Contexts.Main
{
    public sealed class WebApiCallbackHistory : IComparable<WebApiCallbackHistory>
    {
        public Guid CallbackId { get; set; }
        public string Status { get; set; }
        public DateTime InvokedTime { get; set; }
        public byte[] Data { get; set; }

        public WebApiCallback Callback { get; set; }

        public int CompareTo(WebApiCallbackHistory other)
        {
            if (other == null)
            {
                return 1;
            }

            // Check CallbackId
            if (CallbackId != other.CallbackId)
            {
                return CallbackId.CompareTo(other.CallbackId);
            }

            // Check InvokeredTime
            if (InvokedTime != other.InvokedTime)
            {
                return InvokedTime.CompareTo(other.InvokedTime);
            }

            return 0;
        }

        public override bool Equals(object other)
        {
            if (other == null || GetType() != other.GetType())
            {
                return false;
            }

            return CompareTo((WebApiCallbackHistory)other) == 0;
        }

        public override int GetHashCode()
        {
            int hash = 0;

            hash ^= CallbackId.GetHashCode();
            hash ^= InvokedTime.GetHashCode();

            return hash;
        }
    }
}