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
            var recv = new ReceivingAddress(Guid.NewGuid(), address, false, new List<ReceivingAddressReservation>());
            this.receivingAddresses.Add(recv.Id, recv);

            return Task.FromResult(recv);
        }

        public Task<ReceivingAddressReservation> CreateReservationAsync(Guid id, CancellationToken cancellationToken)
        {
            var resv = new ReceivingAddressReservation(Guid.NewGuid(), null, DateTime.UtcNow, null);
            if (this.receivingAddresses.TryGetValue(id, out var recv))
            {

                recv.ReceivingAddressReservations.Add(resv);
            }

            return Task.FromResult(resv);
        }

        public virtual Task<ReceivingAddress> GetAsync(Guid id, CancellationToken cancellationToken)
        {
            if (this.receivingAddresses.TryGetValue(id, out var recv))
            {
                return Task.FromResult(recv);
            }

            return Task.FromResult<ReceivingAddress>(null);
        }

        public Task<ReceivingAddressReservation> GetReservationAsync(Guid id, CancellationToken cancellationToken)
        {
            foreach (var address in this.receivingAddresses)
            {
                foreach (var reservation in address.Value.ReceivingAddressReservations.Where(r => r.Id == id))
                {
                    return Task.FromResult(new ReceivingAddressReservation(reservation.Id, address.Value, reservation.ReservedDate, reservation.ReleasedDate));
                }
            }

            return Task.FromResult<ReceivingAddressReservation>(null);
        }

        public virtual Task<IEnumerable<ReceivingAddress>> ListReceivingAddressAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IEnumerable<ReceivingAddress>>(this.receivingAddresses.Select(a => a.Value).ToList());
        }

        public Task SetLockedStatusAsync(Guid id, bool locked, CancellationToken cancellationToken)
        {
            if (this.receivingAddresses.TryGetValue(id, out var v))
            {
                this.receivingAddresses[v.Id] = new ReceivingAddress(v.Id, v.Address, locked, v.ReceivingAddressReservations);
            }

            return Task.CompletedTask;
        }

        public async Task SetReleasedTimeAsync(Guid id, CancellationToken cancellationToken)
        {
            var reservation = await GetReservationAsync(id, cancellationToken);
            if (reservation != null)
            {
                var recv = this.receivingAddresses[reservation.ReceivingAddress.Id];
                recv.ReceivingAddressReservations.RemoveAll(r => r.Id == id);

                recv.ReceivingAddressReservations.Add(new ReceivingAddressReservation(
                    reservation.Id, reservation.ReceivingAddress, reservation.ReservedDate, DateTime.UtcNow));
            }
        }
    }
}