using System;
using System.Net;

namespace Ztm.WebApi
{
    public class Callback
    {
        public Callback(Guid id, IPAddress requestIp, DateTime requestTime, bool completed, Uri url)
        {
            this.Id = id;
            this.RequestIp = requestIp;
            this.RequestTime = requestTime;
            this.Completed = completed;
            this.Url = url;
        }

        public Guid Id { get; }
        public IPAddress RequestIp { get; }
        public DateTime RequestTime { get; }
        public bool Completed { get; }
        public Uri Url { get; }
    }
}