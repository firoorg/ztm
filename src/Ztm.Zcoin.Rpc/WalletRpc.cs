using System;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using NBitcoin.RPC;

namespace Ztm.Zcoin.Rpc
{
    public sealed class WalletRpc : RpcClient, IWalletRpc
    {
        public WalletRpc(RpcFactory factory, RPCClient client) : base(factory, client)
        {
        }

        public Task<BitcoinAddress> GetNewAddressAsync(CancellationToken cancellationToken)
        {
            return Client.GetNewAddressAsync();
        }

        public Task<uint256> SendAsync(
            BitcoinAddress destination,
            Money amount,
            string comment,
            string commentTo,
            bool subtractFeeFromAmount,
            CancellationToken cancellationToken)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (amount == null)
            {
                throw new ArgumentNullException(nameof(amount));
            }

            if (amount <= Money.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "The value is not valid.");
            }

            return Client.SendToAddressAsync(
                destination,
                amount,
                comment,
                commentTo,
                subtractFeeFromAmount
            );
        }
    }
}
