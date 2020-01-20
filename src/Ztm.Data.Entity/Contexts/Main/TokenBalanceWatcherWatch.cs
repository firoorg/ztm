using System;
using NBitcoin;

namespace Ztm.Data.Entity.Contexts.Main
{
    public sealed class TokenBalanceWatcherWatch
    {
        public Guid Id { get; set; }
        public Guid RuleId { get; set; }
        public uint256 BlockId { get; set; }
        public uint256 TransactionId { get; set; }
        public long BalanceChange { get; set; }
        public DateTime CreatedTime { get; set; }
        public int Confirmation { get; set; }
        public TokenBalanceWatcherWatchStatus Status { get; set; }

        public TokenBalanceWatcherRule Rule { get; set; }
    }

    public enum TokenBalanceWatcherWatchStatus
    {
        Uncompleted,
        Succeeded,
        Rejected,
        TimedOut,
    }
}
