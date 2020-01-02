using System;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using Ztm.Zcoin.Rpc;

namespace Ztm.WebApi.AddressPools
{
    public sealed class RpcAddressGenerator : IAddressGenerator
    {
        readonly IRpcFactory factory;

        public RpcAddressGenerator(IRpcFactory factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            this.factory = factory;
        }

        public async Task<BitcoinAddress> GenerateAsync(CancellationToken cancellationToken)
        {
            using (var client = await this.factory.CreateWalletRpcAsync(cancellationToken))
            {
                return await client.GetNewAddressAsync(cancellationToken);
            }
        }
    }
}
