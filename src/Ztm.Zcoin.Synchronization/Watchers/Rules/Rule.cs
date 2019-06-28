using System;

namespace Ztm.Zcoin.Synchronization.Watchers.Rules
{
    public class Rule
    {
        public Rule() : this(Guid.NewGuid())
        {
        }

        public Rule(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; }

        public override bool Equals(object obj)
        {
            var other = obj as Rule;

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
