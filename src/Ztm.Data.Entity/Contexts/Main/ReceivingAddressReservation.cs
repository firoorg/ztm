using System;

namespace Ztm.Data.Entity.Contexts.Main
{
    public sealed class ReceivingAddressReservation
    {
        public Guid Id { get; set; }
        public Guid AddressId { get; set; }
        public DateTime LockedAt { get; set; }
        public DateTime? ReleasedAt { get; set; }

        public ReceivingAddress Address { get; set; }
    }
}