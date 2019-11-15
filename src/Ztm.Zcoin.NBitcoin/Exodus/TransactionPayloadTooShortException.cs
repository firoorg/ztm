using System;

namespace Ztm.Zcoin.NBitcoin.Exodus
{
    public class TransactionPayloadTooShortException : TransactionTooShortException
    {
        public TransactionPayloadTooShortException(int requiredSize)
            : base(requiredSize + TransactionEncoder.MinSize)
        {
        }

        public TransactionPayloadTooShortException(int requiredSize, Exception innerException)
            : base(requiredSize + TransactionEncoder.MinSize, innerException)
        {
        }
    }
}
