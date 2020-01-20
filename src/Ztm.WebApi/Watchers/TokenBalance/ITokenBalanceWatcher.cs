using System;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.WebApi.Watchers.TokenBalance
{
    public interface ITokenBalanceWatcher
    {
        Task<Rule> WatchAddressAsync(
            BitcoinAddress address,
            PropertyAmount targetAmount,
            int targetConfirmation,
            TimeSpan timeout,
            string timeoutStatus,
            Guid callback,
            CancellationToken cancellationToken);
    }
}
