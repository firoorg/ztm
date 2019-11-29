using System;
using NBitcoin;

namespace Ztm.Data.Entity.Contexts.Main
{
    public sealed class TransactionConfirmationWatch
    {
        public Guid Id { get; set; }
        public Guid RuleId { get; set; }
        public uint256 StartBlock { get; set; }
        public DateTime StartTime { get; set; }
        public uint256 Transaction { get; set; }
        public int Status { get; set; }

        public TransactionConfirmationWatchingRule Rule { get; set; }
    }
}