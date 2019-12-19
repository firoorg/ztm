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
            CallbackResult successResponse,
            CallbackResult timeoutResponse,
            Callback callback,
            DateTime createdAt,
            string note)
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
            this.CreatedAt = createdAt;
            this.Note = note;
        }

        public Guid Id { get; }
        public uint256 Transaction { get; }
        public int Confirmations { get; }
        public TimeSpan OriginalWaitingTime { get; }
        public CallbackResult SuccessResponse { get; }
        public CallbackResult TimeoutResponse { get; }
        public Callback Callback { get; }
        public DateTime CreatedAt { get; }
        public string Note { get; }
    }
}