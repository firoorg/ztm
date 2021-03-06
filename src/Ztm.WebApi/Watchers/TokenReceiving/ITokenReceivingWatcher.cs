using System;
using System.Threading;
using System.Threading.Tasks;
using Ztm.WebApi.AddressPools;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.WebApi.Watchers.TokenReceiving
{
    public interface ITokenReceivingWatcher
    {
        Task<Guid> StartWatchAsync(
            ReceivingAddressReservation address,
            PropertyAmount targetAmount,
            int targetConfirmation,
            TimeSpan timeout,
            TokenReceivingCallback callback,
            CancellationToken cancellationToken);
    }
}
