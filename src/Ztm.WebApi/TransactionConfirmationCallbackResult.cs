using System;

namespace Ztm.WebApi
{
    public sealed class TransactionConfirmationCallbackResult : CallbackResult
    {
        public TransactionConfirmationCallbackResult(string status, string data)
        {
            this.Status = status;
            this.Data = data;
        }

        public override string Status { get; }
        public override object Data { get; }

        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != GetType())
            {
                return false;
            }

            var other = (TransactionConfirmationCallbackResult)obj;
            return this.Status == other.Status && this.Data.Equals(other.Data);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Status, Data);
        }
    }
}