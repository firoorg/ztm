using System;
using NBitcoin;

namespace Ztm.Data.Entity.Contexts.Main
{
    public sealed class TransactionConfirmationWatcherWatch
    {
        public Guid Id { get; set; }
        public Guid RuleId { get; set; }
        public uint256 StartBlockHash { get; set; }
        public DateTime StartTime { get; set; }
        public uint256 TransactionHash { get; set; }
        public TransactionConfirmationWatcherWatchStatus Status { get; set; }

        public TransactionConfirmationWatcherRule Rule { get; set; }
    }

    public enum TransactionConfirmationWatcherWatchStatus
    {
        Pending = 0,
        Succeeded = 1,
        Rejected = 2
    }
}
