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
                await this.storage.AddAddressAsync(address, CancellationToken.None);
        }

        public Task ReleaseAddressAsync(Guid id, CancellationToken cancellationToken)
        {
            return this.ReleaseAddressAsync(id, cancellationToken);
        }

        public async Task<ReceivingAddressReservation> TryLockAddressAsync(CancellationToken cancellationToken)
        {
            var addresses = await this.storage.ListReceivingAddressAsync(cancellationToken);
            var availables = addresses.Where(a => a.Available);

            if (availables.Count() <= 0)
            {
                return null;
            }

            var chosen = this.choser.Choose(availables);
            return await this.storage.TryLockAsync(chosen.Id, CancellationToken.None);
        }
    }
}