using System;
using System.Collections.Generic;
using NBitcoin;

namespace Ztm.WebApi.AddressPools
{
    public sealed class ReceivingAddress
    {
        public Guid Id { get; }
        public BitcoinAddress Address { get; }
        public bool IsLocked { get; }
        public List<ReceivingAddressReservation> ReceivingAddressReservations { get; }

        public ReceivingAddress(Guid id, BitcoinAddress address, bool isLocked, List<ReceivingAddressReservation> receivingAddressReservations)
        {
            this.Id = id;
            this.Address = address;
            this.IsLocked = isLocked;
            this.ReceivingAddressReservations = receivingAddressReservations;
        }

        public bool Available
        {
            get
            {
                return !IsLocked;
            }
        }

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