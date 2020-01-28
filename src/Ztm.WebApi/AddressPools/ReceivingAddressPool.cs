using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ztm.WebApi.AddressPools
{
    public sealed class ReceivingAddressPool : IReceivingAddressPool
    {
        readonly IAddressGenerator generator;
        readonly IReceivingAddressRepository repository;
        readonly IAddressChoser choser;

        public ReceivingAddressPool(
            IAddressGenerator generator,
            IReceivingAddressRepository repository,
            IAddressChoser choser)
        {
            if (generator == null)
            {
                throw new ArgumentNullException(nameof(generator));
            }

            if (repository == null)
            {
                throw new ArgumentNullException(nameof(repository));
            }

            if (choser == null)
            {
                throw new ArgumentNullException(nameof(choser));
            }

            this.generator = generator;
            this.repository = repository;
            this.choser = choser;
        }

        public async Task GenerateAddressAsync(CancellationToken cancellationToken)
        {
            var address = await this.generator.GenerateAsync(cancellationToken);
            await this.repository.AddAsync(address, CancellationToken.None);
        }

        public Task ReleaseAddressAsync(Guid id, CancellationToken cancellationToken)
        {
            return this.repository.ReleaseAsync(id, cancellationToken);
        }

        public async Task<ReceivingAddressReservation> TryLockAddressAsync(CancellationToken cancellationToken)
        {
            var addresses = await this.repository.ListAsync(AddressFilter.Available, cancellationToken);

            if (addresses.Any())
            {
                var chosen = this.choser.Choose(addresses);
                return await this.repository.TryLockAsync(chosen.Id, cancellationToken);
            }

            return null;
        }
    }
}
