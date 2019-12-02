using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ztm.WebApi.AddressPools
{
    public sealed class ReceivingAddressPool : IReceivingAddressPool
    {
        readonly IAddressGenerator generator;
        readonly IReceivingAddressStorage storage;
        readonly IAddressChoser choser;

        public ReceivingAddressPool(
            IAddressGenerator generator,
            IReceivingAddressStorage storage,
            IAddressChoser choser)
        {
            if (generator == null)
            {
                throw new ArgumentNullException(nameof(generator));
            }

            if (storage == null)
            {
                throw new ArgumentNullException(nameof(storage));
            }

            if (choser == null)
            {
                throw new ArgumentNullException(nameof(choser));
            }

            this.generator = generator;
            this.storage = storage;
            this.choser = choser;
        }

        public async Task GenerateAddressAsync(CancellationToken cancellationToken)
        {
            var address = await this.generator.GenerateAsync(cancellationToken);
            await this.storage.AddAsync(address, CancellationToken.None);
        }

        public Task ReleaseAddressAsync(Guid id, CancellationToken cancellationToken)
        {
            return this.storage.ReleaseAsync(id, cancellationToken);
        }

        public async Task<ReceivingAddressReservation> TryLockAddressAsync(CancellationToken cancellationToken)
        {
            var addresses = await this.storage.ListAsync(AddressFilter.Available, cancellationToken);

            if (addresses.Any())
            {
                var chosen = this.choser.Choose(addresses);
                return await this.storage.TryLockAsync(chosen.Id, cancellationToken);
            }

            return null;
        }
    }
}