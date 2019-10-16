using System;
using System.Net;

namespace Ztm.Data.Entity.Contexts.Main
{
    public class CallbackInvocation
    {
        public Guid CallbackId { get; set; }
        public WebApiCallback Callback { get; set; }

        public string Status { get; set; }
        public DateTime InvokedTime { get; set; }
        public byte[] Data { get; set; }
    }
}