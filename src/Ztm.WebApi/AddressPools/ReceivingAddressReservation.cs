using System;

namespace Ztm.WebApi.AddressPools
{
    public sealed class ReceivingAddressReservation
    {
        public Guid Id { get; }
        public ReceivingAddress ReceivingAddress { get; }
        public DateTime ReservedDate { get; }
        public DateTime? ReleasedDate { get; }

        public ReceivingAddressReservation(
            Guid id,
            ReceivingAddress receivingAddress,
            DateTime reservedDate,
            DateTime? releasedDate)
        {
            this.Id = id;
            this.ReceivingAddress = receivingAddress;
            this.ReservedDate = reservedDate;
            this.ReleasedDate = releasedDate;
        }
    }
}