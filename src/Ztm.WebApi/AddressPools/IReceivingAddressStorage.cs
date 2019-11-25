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
        Task SetLockedStatusAsync(Guid id, bool locked, CancellationToken cancellationToken);

        Task<ReceivingAddressReservation> CreateReservationAsync(Guid id, CancellationToken cancellationToken);
        Task<ReceivingAddressReservation> GetReservationAsync(Guid id, CancellationToken cancellationToken);
        Task SetReleasedTimeAsync(Guid id, CancellationToken cancellationToken);
    }
}