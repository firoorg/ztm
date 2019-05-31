using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Ztm.ObjectModel
{
    public class ReferenceEqualityComparer : IEqualityComparer
    {
        public static ReferenceEqualityComparer Default { get; } = new ReferenceEqualityComparer();

        public new bool Equals(object x, object y)
        {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(object obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }

    public class ReferenceEqualityComparer<T> : ReferenceEqualityComparer, IEqualityComparer<T> where T : class
    {
        public new static ReferenceEqualityComparer<T> Default { get; } = new ReferenceEqualityComparer<T>();

        public bool Equals(T x, T y)
        {
            return base.Equals(x, y);
        }

        public int GetHashCode(T obj)
        {
            return base.GetHashCode(obj);
        }
    }
}
