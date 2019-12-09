using System;
using System.Net;

namespace Ztm.WebApi.Callbacks
{
    public class Callback : IComparable<Callback>
    {
        public Callback(Guid id, IPAddress registeredIp, DateTime registeredTime, bool completed, Uri url)
        {
            if (registeredIp == null)
            {
                throw new ArgumentNullException(nameof(registeredIp));
            }

            if (url == null)
            {
                throw new ArgumentNullException(nameof(url));
            }

            this.Id = id;
            this.RegisteredIp = registeredIp;
            this.RegisteredTime = registeredTime;
            this.Completed = completed;
            this.Url = url;
        }

        public Guid Id { get; }
        public IPAddress RegisteredIp { get; }
        public DateTime RegisteredTime { get; }
        public bool Completed { get; }
        public Uri Url { get; }

        public int CompareTo(Callback other)
        {
            if (other == null)
            {
                return 1;
            }

            return Id.CompareTo(other.Id);
        }

        public override bool Equals(object other)
        {
            if (other == null || other.GetType() != GetType())
            {
                return false;
            }

            return CompareTo((Callback)other) == 0;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}