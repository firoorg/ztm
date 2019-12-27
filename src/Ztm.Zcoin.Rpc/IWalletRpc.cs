using System;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;

namespace Ztm.Zcoin.Rpc
{
    public interface IWalletRpc : IDisposable
    {
        Task<BitcoinAddress> GetNewAddressAsync(CancellationToken cancellationToken);

        Task<uint256> SendAsync(
            BitcoinAddress destination,
            Money amount,
            string comment,
            string commentTo,
            bool subtractFeeFromAmount,
            CancellationToken cancellationToken);
    }
}
