using System;
using System.Net;
using NBitcoin;

namespace Ztm.Data.Entity.Contexts.Main
{
    public class WebApiCallback
    {
        public Guid Id { get; set; }
        public IPAddress RequestIp { get; set; }
        public DateTime RequestTime { get; set; }
        public uint256 TransactionId { get; set; }
        public Uri Url { get; set; }
    }
}
