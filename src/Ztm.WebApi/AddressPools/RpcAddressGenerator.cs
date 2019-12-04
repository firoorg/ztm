using System;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using Ztm.Zcoin.Rpc;

namespace Ztm.WebApi.AddressPools
{
    public sealed class RpcAddressGenerator : IAddressGenerator
    {
        readonly IZcoinRpcClient client;

        public RpcAddressGenerator(IZcoinRpcClient client)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            this.client = client;
        }

        public Task<BitcoinAddress> GenerateAsync(CancellationToken cancellationToken)
        {
            return this.client.GetNewAddressAsync(cancellationToken);
        }
    }
}