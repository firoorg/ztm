using System;
using NBitcoin;

namespace Ztm.WebApi
{
    public class TransactionConfirmationWatch<TCallbackResult>
    {
        public TransactionConfirmationWatch(
            Guid id,
            uint256 transaction,
            int confirmation,
            DateTime due,
            TCallbackResult success,
            TCallbackResult timeout,
            Callback callback)
        {
            this.Id = id;
            this.Transaction = transaction;
            this.Confirmation = confirmation;
            this.Due = due;
            this.Success = success;
            this.Timeout = timeout;
            this.Callback = callback;
        }

        public Guid Id { get; }
        public uint256 Transaction { get; }
        public int Confirmation { get; }
        public DateTime Due { get; }
        public TCallbackResult Success;
        public TCallbackResult Timeout;
        public Callback Callback { get; }
    }
}