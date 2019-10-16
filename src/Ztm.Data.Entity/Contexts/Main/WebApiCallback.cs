using System;
using System.Collections.Generic;
using System.Net;

namespace Ztm.Data.Entity.Contexts.Main
{
    public class WebApiCallback
    {
        public WebApiCallback()
        {
            Invocations = new List<CallbackInvocation>();
        }

        public Guid Id { get; set; }
        public IPAddress RequestIp { get; set; }
        public DateTime RequestTime { get; set; }
        public bool Completed { get; set; }
        public Uri Url { get; set; }

        public List<CallbackInvocation> Invocations { get; set; }
    }
}
