using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ztm.WebApi.AddressPools
{
    public interface IReceivingAddressPool
    {
        Task<ReceivingAddress> GenerateAddressAsync(CancellationToken cancellationToken);

        Task ReleaseAddressAsync(Guid id, CancellationToken cancellationToken);

        Task<ReceivingAddressReservation> TryLockAddressAsync(CancellationToken cancellationToken);
    }
}
