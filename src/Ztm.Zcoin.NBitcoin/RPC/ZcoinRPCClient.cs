using System;
using NBitcoin;
using NBitcoin.RPC;

namespace Ztm.Zcoin.NBitcoin.RPC
{
    public class ZcoinRPCClient : RPCClient
    {
        public ZcoinRPCClient(string authenticationString, Uri address, Network network = null)
            : base(authenticationString, address, network)
        {
        }
    }
}
