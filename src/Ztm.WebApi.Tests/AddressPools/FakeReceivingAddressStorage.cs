using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using Ztm.WebApi.AddressPools;

namespace Ztm.WebApi.Tests.AddressPools
{
    public class FakeReceivingAddressStorage : IReceivingAddressStorage
    {
        readonly Dictionary<Guid, ReceivingAddress> receivingAddresses;

        public FakeReceivingAddressStorage()
        {
            this.receivingAddresses = new Dictionary<Guid, ReceivingAddress>();
        }

        public virtual Task<ReceivingAddress> AddAsync(BitcoinAddress address, CancellationToken cancellationToken)
        {
            var recv = new ReceivingAddress(Guid.NewGuid(), address, false, new Collection<ReceivingAddressReservation>());
            this.receivingAddresses.Add(recv.Id, recv);

            return Task.FromResult(recv);
        }

        public virtual Task<ReceivingAddress> GetAsync(Guid id, CancellationToken cancellationToken)
        {
            if (this.receivingAddresses.TryGetValue(id, out var recv))
            {
                return Task.FromResult(recv);
            }

            return Task.FromResult<ReceivingAddress>(null);
        }

        public virtual Task<IEnumerable<ReceivingAddress>> ListAsync(AddressFilter filter, CancellationToken cancellationToken)
        {
            var addresses = this.receivingAddresses.AsEnumerable();

            if (filter.HasFlag(AddressFilter.Available))
            {
                addresses = addresses.Where(e => !e.Value.IsLocked);
            }

            if (filter.HasFlag(AddressFilter.NeverUsed))
            {
                addresses = addresses.Where(e => e.Value.Reservations.Count == 0);
            }

            return Task.FromResult<IEnumerable<ReceivingAddress>>(addresses.Select(a => a.Value).ToList());
        }

        public virtual Task ReleaseAsync(Guid id, CancellationToken cancellationToken)
        {
            if (this.receivingAddresses.TryGetValue(id, out var recv))
            {
                var reservations = recv.Reservations;

                var last = reservations.Last();
                reservations.Remove(last);
                reservations.Add(new ReceivingAddressReservation(last.Id, last.Address, last.ReservedDate, DateTime.UtcNow));

                var updated = new ReceivingAddress(recv.Id, recv.Address, false, reservations);

                this.receivingAddresses.Remove(id);
                this.receivingAddresses.Add(id, updated);
            }

            return Task.CompletedTask;
        }

        public virtual Task<ReceivingAddressReservation> TryLockAsync(Guid id, CancellationToken cancellationToken)
        {
            if (this.receivingAddresses.TryGetValue(id, out var recv))
            {
                if (recv.IsLocked)
                {
                    throw new InvalidOperationException();
                }

                var lockedAt = DateTime.UtcNow;
                var reservation = new ReceivingAddressReservation(Guid.NewGuid(), recv, lockedAt, DateTime.MinValue);
                var reservations = recv.Reservations;

                reservations.Add(reservation);
                var updated = new ReceivingAddress(recv.Id, recv.Address, true, reservations);

                this.receivingAddresses.Remove(id);
                this.receivingAddresses.Add(id, updated);

                return Task.FromResult(reservation);
            }

            return Task.FromResult<ReceivingAddressReservation>(null);
        }
    }
}