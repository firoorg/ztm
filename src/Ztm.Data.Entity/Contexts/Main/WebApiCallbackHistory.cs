using System;

namespace Ztm.Data.Entity.Contexts.Main
{
    public sealed class WebApiCallbackHistory : IComparable<WebApiCallbackHistory>
    {
        public int Id { get; set; }
        public Guid CallbackId { get; set; }
        public string Status { get; set; }
        public DateTime InvokedTime { get; set; }

        /// <value>
        /// A JSON represents the callback data.
        /// </value>
        public string Data { get; set; }

        public WebApiCallback Callback { get; set; }

        public int CompareTo(WebApiCallbackHistory other)
        {
            if (other == null)
            {
                return 1;
            }

            return Id.CompareTo(other.Id);
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
            return Id;
        }
    }
}