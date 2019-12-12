using System;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using Ztm.Zcoin.Rpc;

namespace Ztm.WebApi.AddressPools
{
    public sealed class RpcAddressGenerator : IAddressGenerator
    {
        readonly IZcoinRpcClientFactory factory;

        public RpcAddressGenerator(IZcoinRpcClientFactory factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            this.factory = factory;
        }

        public async Task<BitcoinAddress> GenerateAsync(CancellationToken cancellationToken)
        {
            using (var client = await this.factory.CreateRpcClientAsync(cancellationToken))
            {
                return await client.GetNewAddressAsync(cancellationToken);
            }
        }
    }
}