using System;
using NBitcoin;
using Ztm.WebApi.Callbacks;

namespace Ztm.WebApi.TransactionConfirmationWatchers
{
    public class Rule
    {
        public Rule(
            Guid id,
            uint256 transaction,
            RuleStatus status,
            int confirmations,
            TimeSpan waitingTime,
            dynamic successResponse,
            dynamic timeoutResponse,
            Callback callback,
            Guid? currentWatchId)
        {
            if (transaction == null)
            {
                throw new ArgumentNullException(nameof(transaction));
            }

            if (successResponse == null)
            {
                throw new ArgumentNullException(nameof(successResponse));
            }

            if (timeoutResponse == null)
            {
                throw new ArgumentNullException(nameof(timeoutResponse));
            }

            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            if (confirmations <= 0)
            {
                throw new ArgumentException("The confirmations is lesser than 1.", nameof(confirmations));
            }

            if (waitingTime < TimeSpan.Zero)
            {
                throw new ArgumentException("The waitingTime is negative.", nameof(waitingTime));
            }

            this.Id = id;
            this.Transaction = transaction;
            this.Status = status;
            this.Confirmations = confirmations;
            this.WaitingTime = waitingTime;
            this.SuccessResponse = successResponse;
            this.TimeoutResponse = timeoutResponse;
            this.Callback = callback;
            this.CurrentWatchId = currentWatchId;
        }

        public Guid Id { get; }
        public uint256 Transaction { get; }
        public RuleStatus Status { get; }
        public int Confirmations { get; }
        public TimeSpan WaitingTime { get; }
        public dynamic SuccessResponse { get; }
        public dynamic TimeoutResponse { get; }
        public Callback Callback { get; }
        public Guid? CurrentWatchId { get; }
    }
}