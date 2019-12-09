using System;
using NBitcoin;
using Ztm.WebApi.Callbacks;

namespace Ztm.WebApi.TransactionConfirmationWatchers
{
    public class Rule<TCallbackResult>
    {
        public Rule(
            Guid id,
            uint256 transaction,
            RuleStatus status,
            int confirmation,
            TimeSpan waitingTime,
            TCallbackResult success,
            TCallbackResult timeout,
            Callback callback,
            Guid? currentWatchId)
        {
            this.Id = id;
            this.Transaction = transaction;
            this.Status = status;
            this.Confirmation = confirmation;
            this.WaitingTime = waitingTime;
            this.Success = success;
            this.Timeout = timeout;
            this.Callback = callback;
            this.CurrentWatchId = currentWatchId;
        }

        public Guid Id { get; }
        public uint256 Transaction { get; }
        public RuleStatus Status { get; }
        public int Confirmation { get; }
        public TimeSpan WaitingTime { get; }
        public TCallbackResult Success { get; }
        public TCallbackResult Timeout { get; }
        public Callback Callback { get; }
        public Guid? CurrentWatchId { get; }
    }
}