using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;

namespace Ztm.Testing
{
    public static class EqualityTesting
    {
        public static void TestEquals<T>(T subject, params Func<T, T>[] equal)
        {
            TestEquals(subject, equal.AsEnumerable());
        }

        public static void TestEquals<T>(T subject, IEnumerable<Func<T, T>> equal)
        {
            if (subject == null)
            {
                throw new ArgumentNullException(nameof(subject));
            }

            if (equal == null)
            {
                throw new ArgumentNullException(nameof(equal));
            }

            var equatable = subject as IEquatable<T>;

            foreach (var v in equal.Select(f => f(subject)))
            {
                subject.Equals(v).Should().BeTrue();
                equatable?.Equals(v).Should().BeTrue();
            }
        }

        public static void TestInequal<T>(T subject, params Func<T, T>[] inequal)
        {
            TestInequal(subject, inequal.AsEnumerable());
        }

        public static void TestInequal<T>(T subject, IEnumerable<Func<T, T>> inequal)
        {
            if (subject == null)
            {
                throw new ArgumentNullException(nameof(subject));
            }

            if (inequal == null)
            {
                throw new ArgumentNullException(nameof(inequal));
            }

            subject.Equals(null).Should().BeFalse();
            subject.Equals(new {}).Should().BeFalse();

            var equatable = subject as IEquatable<T>;

            foreach (var v in inequal.Select(f => f(subject)))
            {
                subject.Equals(v).Should().BeFalse();
                equatable?.Equals(v).Should().BeFalse();
            }
        }
    }
}
