using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;

namespace Ztm.WebApi.AddressPools
{
    public interface IReceivingAddressStorage
    {
        Task<ReceivingAddress> AddAddressAsync(BitcoinAddress address, CancellationToken cancellationToken);
        Task<ReceivingAddress> GetAsync(Guid id, CancellationToken cancellationToken);
        Task<IEnumerable<ReceivingAddress>> ListReceivingAddressAsync(CancellationToken cancellationToken);
        Task<ReceivingAddressReservation> TryLockAsync(Guid id, TimeSpan timeout, CancellationToken cancellationToken);
        Task ReleaseAsync(Guid id, CancellationToken cancellationToken);
    }
}