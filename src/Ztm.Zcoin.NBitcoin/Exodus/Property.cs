using System;

namespace Ztm.Zcoin.NBitcoin.Exodus
{
    public sealed class Property
    {
        public Property(PropertyId id, PropertyType type)
        {
            if (id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            Id = id;
            Type = type;
        }

        public PropertyId Id { get; }

        public PropertyType Type { get; }

        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != GetType())
            {
                return false;
            }

            return ((Property)obj).Id == Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
