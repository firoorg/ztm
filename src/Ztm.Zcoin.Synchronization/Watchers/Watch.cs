using System;
using NBitcoin;

namespace Ztm.Zcoin.Synchronization.Watchers
{
    public class Watch : IEquatable<Watch>
    {
        public Watch(uint256 startBlock) : this(startBlock, DateTime.Now)
        {
        }

        public Watch(uint256 startBlock, DateTime startTime) : this(startBlock, startTime, Guid.NewGuid())
        {
        }

        public Watch(uint256 startBlock, DateTime startTime, Guid id)
        {
            if (startBlock == null)
            {
                throw new ArgumentNullException(nameof(startBlock));
            }

            StartBlock = startBlock;
            StartTime = startTime;
            Id = id;
        }

        public Guid Id { get; }

        public uint256 StartBlock { get; }

        public DateTime StartTime { get; }

        public bool Equals(Watch other)
        {
            if (other == null || other.GetType() != GetType())
            {
                return false;
            }

            return other.Id == Id && other.StartBlock == StartBlock && other.StartTime == StartTime;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((Watch)obj);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
