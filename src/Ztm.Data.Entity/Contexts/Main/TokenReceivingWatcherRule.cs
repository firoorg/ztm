using System;
using System.Collections.ObjectModel;

namespace Ztm.Data.Entity.Contexts.Main
{
    public sealed class TokenReceivingWatcherRule
    {
        public Guid Id { get; set; }
        public Guid CallbackId { get; set; }
        public long PropertyId { get; set; }
        public Guid AddressReservationId { get; set; }
        public long TargetAmount { get; set; }
        public int TargetConfirmation { get; set; }
        public TimeSpan OriginalTimeout { get; set; }
        public TimeSpan CurrentTimeout { get; set; }
        public string TimeoutStatus { get; set; }
        public TokenReceivingWatcherRuleStatus Status { get; set; }

        public WebApiCallback Callback { get; set; }
        public ReceivingAddressReservation AddressReservation { get; set; }
        public Collection<TokenReceivingWatcherWatch> Watches { get; set; }
    }

    public enum TokenReceivingWatcherRuleStatus
    {
        Uncompleted,
        Succeeded,
        TimedOut,
    }
}
