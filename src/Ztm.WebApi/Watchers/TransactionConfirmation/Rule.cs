using System;
using NBitcoin;
using Ztm.WebApi.Callbacks;

namespace Ztm.WebApi.Watchers.TransactionConfirmation
{
    public class Rule
    {
        public Rule(
            Guid id,
            uint256 transactionHash,
            int confirmations,
            TimeSpan originalWaitingTime,
            CallbackResult successResponse,
            CallbackResult timeoutResponse,
            Callback callback,
            DateTime createdAt)
        {
            if (transactionHash == null)
            {
                throw new ArgumentNullException(nameof(transactionHash));
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
            this.TransactionHash = transactionHash;
            this.Confirmations = confirmations;
            this.OriginalWaitingTime = originalWaitingTime;
            this.SuccessResponse = successResponse;
            this.TimeoutResponse = timeoutResponse;
            this.Callback = callback;
            this.CreatedAt = createdAt;
        }

        public Guid Id { get; }
        public uint256 TransactionHash { get; }
        public int Confirmations { get; }
        public TimeSpan OriginalWaitingTime { get; }
        public CallbackResult SuccessResponse { get; }
        public CallbackResult TimeoutResponse { get; }
        public Callback Callback { get; }
        public DateTime CreatedAt { get; }
    }
}