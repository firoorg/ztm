using System;

namespace Ztm.Zcoin.NBitcoin.Exodus
{
    public class TransactionTooShortException : TransactionException
    {
        new const string Message = "Transaction data is too short.";

        public TransactionTooShortException(int requiredSize) : base(Message)
        {
            if (requiredSize < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(requiredSize),
                    requiredSize,
                    "The value is lower than one."
                );
            }

            RequiredSize = requiredSize;
        }

        public TransactionTooShortException(int requiredSize, Exception innerException) : base(Message, innerException)
        {
            if (requiredSize < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(requiredSize),
                    requiredSize,
                    "The value is lower than one."
                );
            }

            RequiredSize = requiredSize;
        }

        public int RequiredSize { get; }
    }
}
