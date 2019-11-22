using System;
using NBitcoin;

namespace Ztm.WebApi
{
    public class TransactionConfirmationWatchingRule<TCallbackResult>
    {
        public TransactionConfirmationWatchingRule(
            Guid id,
            uint256 transaction,
            bool completed,
            int confirmation,
            TimeSpan waitingTime,
            TCallbackResult success,
            TCallbackResult timeout,
            Callback callback)
        {
            this.Id = id;
            this.Transaction = transaction;
            this.Completed = completed;
            this.Confirmation = confirmation;
            this.WaitingTime = waitingTime;
            this.Success = success;
            this.Timeout = timeout;
            this.Callback = callback;
        }

        public Guid Id { get; }
        public uint256 Transaction { get; }
        public bool Completed { get; }
        public int Confirmation { get; }
        public TimeSpan WaitingTime { get; }
        public TCallbackResult Success;
        public TCallbackResult Timeout;
        public Callback Callback { get; }
    }
}