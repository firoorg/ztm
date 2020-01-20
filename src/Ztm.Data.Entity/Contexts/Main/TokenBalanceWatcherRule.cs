using System;
using System.Collections.ObjectModel;

namespace Ztm.Data.Entity.Contexts.Main
{
    public sealed class TokenBalanceWatcherRule
    {
        public Guid Id { get; set; }
        public Guid CallbackId { get; set; }
        public long PropertyId { get; set; }
        public string Address { get; set; }
        public long TargetAmount { get; set; }
        public int TargetConfirmation { get; set; }
        public TimeSpan OriginalTimeout { get; set; }
        public TimeSpan CurrentTimeout { get; set; }
        public string TimeoutStatus { get; set; }
        public TokenBalanceWatcherRuleStatus Status { get; set; }

        public WebApiCallback Callback { get; set; }
        public Collection<TokenBalanceWatcherWatch> Watches { get; set; }
    }

    public enum TokenBalanceWatcherRuleStatus
    {
        Uncompleted,
        Succeeded,
        TimedOut,
    }
}
