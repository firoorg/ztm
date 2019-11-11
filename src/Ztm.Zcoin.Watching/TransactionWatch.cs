using System;
using NBitcoin;

namespace Ztm.Zcoin.Watching
{
    public sealed class TransactionWatch<T> : Watch<T>
    {
        public TransactionWatch(T context, uint256 startBlock, uint256 txId)
            : base(context, startBlock)
        {
            if (txId == null)
            {
                throw new ArgumentNullException(nameof(txId));
            }

            TransactionId = txId;
        }

        public TransactionWatch(T context, uint256 startBlock, uint256 txId, DateTime startTime)
            : base(context, startBlock, startTime)
        {
            if (txId == null)
            {
                throw new ArgumentNullException(nameof(txId));
            }

            TransactionId = txId;
        }

        public TransactionWatch(T context, uint256 startBlock, uint256 txId, DateTime startTime, Guid id)
            : base(context, startBlock, startTime, id)
        {
            if (txId == null)
            {
                throw new ArgumentNullException(nameof(txId));
            }

            TransactionId = txId;
        }

        public uint256 TransactionId { get; }
    }
}
