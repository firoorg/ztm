using System;
using NBitcoin;

namespace Ztm.Data.Entity.Contexts.Main
{
    public sealed class TransactionConfirmationWatcherWatch
    {
        public Guid Id { get; set; }
        public Guid RuleId { get; set; }
        public uint256 StartBlock { get; set; }
        public DateTime StartTime { get; set; }
        public uint256 Transaction { get; set; }
        public int Status { get; set; }

        public TransactionConfirmationWatcherRule Rule { get; set; }
    }
}