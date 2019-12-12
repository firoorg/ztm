using System;
using System.Collections.Generic;
using NBitcoin;

namespace Ztm.WebApi.AddressPools
{
    public sealed class ReceivingAddress
    {
        public ReceivingAddress(Guid id, BitcoinAddress address, bool isLocked, ICollection<ReceivingAddressReservation> reservations)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            if (reservations == null)
            {
                throw new ArgumentNullException(nameof(reservations));
            }

            this.Id = id;
            this.Address = address;
            this.IsLocked = isLocked;
            this.Reservations = reservations;
        }

        public BitcoinAddress Address { get; }
        public Guid Id { get; }
        public bool IsLocked { get; }
        public ICollection<ReceivingAddressReservation> Reservations { get; }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            return this.Id.Equals(((ReceivingAddress)obj).Id);
        }

        public override int GetHashCode()
        {
            return this.Id.GetHashCode();
        }
    }
}