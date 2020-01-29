using System;
using NBitcoin;

namespace Ztm.Data.Entity.Contexts.Main
{
    public sealed class TokenReceivingWatcherWatch
    {
        public Guid Id { get; set; }
        public Guid RuleId { get; set; }
        public uint256 BlockId { get; set; }
        public uint256 TransactionId { get; set; }
        public long BalanceChange { get; set; }
        public DateTime CreatedTime { get; set; }
        public int Confirmation { get; set; }
        public TokenReceivingWatcherWatchStatus Status { get; set; }

        public TokenReceivingWatcherRule Rule { get; set; }
        public Block Block { get; set; }
        public Transaction Transaction { get; set; }
    }

    public enum TokenReceivingWatcherWatchStatus
    {
        Uncompleted,
        Succeeded,
        Rejected,
        TimedOut,
    }
}
