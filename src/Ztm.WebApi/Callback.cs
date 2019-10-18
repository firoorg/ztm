using System;
using System.Net;

namespace Ztm.WebApi
{
    public class Callback
    {
        public Callback(Guid id, IPAddress registeredIp, DateTime registeredTime, bool completed, Uri url)
        {
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
    }
}