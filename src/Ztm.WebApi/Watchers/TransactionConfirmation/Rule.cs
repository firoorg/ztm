using System;
using NBitcoin;
using Ztm.WebApi.Callbacks;

namespace Ztm.WebApi.Watchers.TransactionConfirmation
{
    public class Rule
    {
        public Rule(
            Guid id,
            uint256 transaction,
            int confirmations,
            TimeSpan originalWaitingTime,
            dynamic successResponse,
            dynamic timeoutResponse,
            Callback callback)
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

            if (originalWaitingTime < TimeSpan.Zero)
            {
                throw new ArgumentException("The waitingTime is negative.", nameof(originalWaitingTime));
            }

            this.Id = id;
            this.Transaction = transaction;
            this.Confirmations = confirmations;
            this.OriginalWaitingTime = originalWaitingTime;
            this.SuccessResponse = successResponse;
            this.TimeoutResponse = timeoutResponse;
            this.Callback = callback;
        }

        public Guid Id { get; }
        public uint256 Transaction { get; }
        public int Confirmations { get; }
        public TimeSpan OriginalWaitingTime { get; }
        public dynamic SuccessResponse { get; }
        public dynamic TimeoutResponse { get; }
        public Callback Callback { get; }
    }
}