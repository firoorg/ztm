using System;
using NBitcoin;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.Configuration
{
    public class ZcoinConfiguration
    {
        public ZcoinNetworkConfiguration Network { get; set; }
        public ZcoinRpcConfiguration Rpc { get; set; }
        public ZcoinPropertyConfiguration Property { get; set; }
        public ZcoinZeroMqConfiguration ZeroMq { get; set; }
    }

    public class ZcoinNetworkConfiguration
    {
        public NetworkType Type { get; set; }
    }

    public class ZcoinRpcConfiguration
    {
        public Uri Address { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
    }

    public class ZcoinPropertyConfiguration
    {
        public PropertyId Id { get; set; }
        public PropertyType Type { get; set; }
        public BitcoinAddressConfiguration Issuer { get; set; }
        public BitcoinAddressConfiguration Distributor { get; set; }
    }

    public class ZcoinZeroMqConfiguration
    {
        public string Address { get; set; }
    }
}
