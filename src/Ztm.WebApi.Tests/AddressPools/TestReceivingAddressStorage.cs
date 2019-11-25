using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using Ztm.WebApi.AddressPools;

namespace Ztm.WebApi.Tests.AddressPools
{
    public class TestReceivingAddressStorage : IReceivingAddressStorage
    {
        readonly Dictionary<Guid, ReceivingAddress> receivingAddresses;
        public TestReceivingAddressStorage()
        {
            this.receivingAddresses = new Dictionary<Guid, ReceivingAddress>();
        }

        public virtual Task<ReceivingAddress> AddAddressAsync(BitcoinAddress address, CancellationToken cancellationToken)
        {
            var recv = new ReceivingAddress(Guid.NewGuid(), address, false, null);
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

        public virtual Task<IEnumerable<ReceivingAddress>> ListReceivingAddressAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IEnumerable<ReceivingAddress>>(this.receivingAddresses.Select(a => a.Value).ToList());
        }

        public virtual Task ReleaseAsync(Guid id, CancellationToken cancellationToken)
        {
            if (this.receivingAddresses.TryGetValue(id, out var recv))
            {
                var reservations = recv.ReceivingAddressReservations == null
                    ? new List<ReceivingAddressReservation>()
                    : recv.ReceivingAddressReservations;

                var last = reservations.Last();
                reservations[reservations.Count - 1] = new ReceivingAddressReservation(last.Id, last.ReceivingAddress, last.ReservedDate, DateTime.UtcNow);

                var updated = new ReceivingAddress(recv.Id, recv.Address, false, reservations);

                this.receivingAddresses.AddOrReplace(id, updated);
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

                var reservations = recv.ReceivingAddressReservations == null
                    ? new List<ReceivingAddressReservation>()
                    : recv.ReceivingAddressReservations;

                reservations.Add(reservation);
                var updated = new ReceivingAddress(recv.Id, recv.Address, true, reservations);

                this.receivingAddresses.AddOrReplace(id, updated);

                return Task.FromResult(reservation);
            }

            return Task.FromResult<ReceivingAddressReservation>(null);
        }
    }
}