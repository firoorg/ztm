using System;
using System.Collections.Generic;
using System.Net;

namespace Ztm.Data.Entity.Contexts.Main
{
    public sealed class WebApiCallback
    {
        public WebApiCallback()
        {
            InvocationHistories = new SortedSet<WebApiCallbackHistory>();
        }

        public Guid Id { get; set; }
        public IPAddress RegisteredIp { get; set; }
        public DateTime RegisteredTime { get; set; }
        public bool Completed { get; set; }
        public Uri Url { get; set; }

        public SortedSet<WebApiCallbackHistory> InvocationHistories { get; set; }
    }
}
