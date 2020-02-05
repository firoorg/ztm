using System;
using NBitcoin;

namespace Ztm.WebApi.Models
{
    public class ReceivingResponse
    {
        public BitcoinAddress Address { get; set; }

        public DateTime Deadline { get; set; }
    }
}
