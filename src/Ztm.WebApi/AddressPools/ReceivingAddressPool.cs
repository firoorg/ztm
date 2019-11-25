using System;
using System.Collections.Generic;
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

        public async Task ReleaseAsync(Guid id, CancellationToken cancellationToken)
        {
            this.storageLock.EnterWriteLock();

            try
            {
                var r = await this.storage.GetReservationAsync(id, cancellationToken);
                if (r == null)
                {
                    throw new KeyNotFoundException("The reservation is not found.");
                }

                if (r.ReleasedDate != null)
                {
                    throw new InvalidOperationException("The reservation is already released.");
                }

                await this.storage.SetLockedStatusAsync(r.ReceivingAddress.Id, false, cancellationToken);
                await this.storage.SetReleasedTimeAsync(id, cancellationToken);
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
                        var reservation = await this.storage.CreateReservationAsync(chosen.Id, cancellationToken);
                        await this.storage.SetLockedStatusAsync(chosen.Id, true, cancellationToken);

                        return reservation;
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