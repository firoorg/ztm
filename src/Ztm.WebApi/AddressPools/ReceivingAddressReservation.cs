using System;

namespace Ztm.WebApi.AddressPools
{
    public sealed class ReceivingAddressReservation
    {
        public ReceivingAddressReservation(
            Guid id,
            ReceivingAddress address,
            DateTime reservedDate,
            DateTime? releasedDate)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            this.Id = id;
            this.Address = address;
            this.ReservedDate = reservedDate;
            this.ReleasedDate = releasedDate;
        }

        public ReceivingAddress Address { get; }
        public Guid Id { get; }
        public DateTime? ReleasedDate { get; }
        public DateTime ReservedDate { get; }
    }
}