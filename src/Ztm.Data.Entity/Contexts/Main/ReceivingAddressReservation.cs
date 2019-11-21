using System;

namespace Ztm.Data.Entity.Contexts.Main
{
    public sealed class ReceivingAddressReservation
    {
        public Guid Id { get; set; }
        public Guid ReceivingAddressId { get; set; }
        public DateTime LockedAt { get; set; }
        public DateTime ReleasedAt { get; set; }
        public DateTime Due { get; set; }

        public ReceivingAddress ReceivingAddress { get; set; }
    }
}