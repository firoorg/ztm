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

        readonly ReaderWriterLockSlim storageLock;

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

            this.storageLock = new ReaderWriterLockSlim();
        }

        public async Task GenerateAddressAsync(CancellationToken cancellationToken)
        {
            storageLock.EnterWriteLock();

            try
            {
                var address = await this.generator.GenerateAsync(cancellationToken);
                await this.storage.AddAddressAsync(address, cancellationToken);
            }
            finally
            {
                storageLock.ExitWriteLock();
            }
        }

        public Task ReleaseAddressAsync(Guid id, CancellationToken cancellationToken)
        {
            this.storageLock.EnterWriteLock();

            try
            {
                return this.ReleaseAddressAsync(id, cancellationToken);
            }
            finally
            {
                this.storageLock.ExitWriteLock();
            }
        }

        public async Task<ReceivingAddressReservation> TryLockAddressAsync(CancellationToken cancellationToken)
        {
            this.storageLock.EnterWriteLock();

            try
            {
                var addresses = await this.storage.ListReceivingAddressAsync(cancellationToken);
                if (addresses != null)
                {
                    var availables = addresses.Where(a => a.Available);

                    if (availables.Count() <= 0)
                    {
                        return null;
                    }

                    var chosen = this.choser.Choose(availables);

                    if (chosen != null)
                    {
                        return await this.storage.TryLockAsync(chosen.Id, CancellationToken.None);
                    }
                }
            }
            finally
            {
                this.storageLock.ExitWriteLock();
            }

            return null;
        }
    }
}