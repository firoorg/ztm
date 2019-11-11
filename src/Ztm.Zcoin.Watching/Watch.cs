using System;
using NBitcoin;

namespace Ztm.Zcoin.Watching
{
    public class Watch<T>
    {
        public Watch(T context, uint256 startBlock)
            : this(context, startBlock, DateTime.Now)
        {
        }

        public Watch(T context, uint256 startBlock, DateTime startTime)
            : this(context, startBlock, startTime, Guid.NewGuid())
        {
        }

        public Watch(T context, uint256 startBlock, DateTime startTime, Guid id)
        {
            if (startBlock == null)
            {
                throw new ArgumentNullException(nameof(startBlock));
            }

            Context = context;
            StartBlock = startBlock;
            StartTime = startTime;
            Id = id;
        }

        public T Context { get; }

        public Guid Id { get; }

        public uint256 StartBlock { get; }

        public DateTime StartTime { get; }

        public override bool Equals(object obj)
        {
            var other = obj as Watch<T>;

            if (other == null || other.GetType() != GetType())
            {
                return false;
            }

            return other.Id == Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
