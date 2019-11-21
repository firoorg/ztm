using System;
using System.Collections.Generic;

namespace Ztm.Data.Entity.Contexts.Main
{
    public sealed class ReceivingAddress
    {
        public Guid Id { get; set; }
        public string Address { get; set; }
        public bool IsLocked { get; set; }

        public List<ReceivingAddressReservation> ReceivingAddressReservations { get; set; }
    }
}