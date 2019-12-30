using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;

namespace Ztm.Testing
{
    public static class EqualityTesting
    {
        public static void TestEquals<T>(T subject, params Func<T, T>[] equals)
        {
            TestEquals(subject, equals.AsEnumerable());
        }

        public static void TestEquals<T>(T subject, IEnumerable<Func<T, T>> equals)
        {
            if (subject == null)
            {
                throw new ArgumentNullException(nameof(subject));
            }

            if (equals == null)
            {
                throw new ArgumentNullException(nameof(equals));
            }

            var equatable = subject as IEquatable<T>;

            foreach (var v in equals.Select(f => f(subject)))
            {
                subject.Equals(v).Should().BeTrue();
                equatable?.Equals(v).Should().BeTrue();
            }
        }

        public static void TestInequal<T>(T subject, params Func<T, T>[] inequals)
        {
            TestInequal(subject, inequals.AsEnumerable());
        }

        public static void TestInequal<T>(T subject, IEnumerable<Func<T, T>> inequals)
        {
            if (subject == null)
            {
                throw new ArgumentNullException(nameof(subject));
            }

            if (inequals == null)
            {
                throw new ArgumentNullException(nameof(inequals));
            }

            subject.Equals(null).Should().BeFalse(); // lgtm[cs/null-argument-to-equals]
            subject.Equals(new {}).Should().BeFalse();

            var equatable = subject as IEquatable<T>;

            foreach (var v in inequals.Select(f => f(subject)))
            {
                subject.Equals(v).Should().BeFalse();
                equatable?.Equals(v).Should().BeFalse();
            }
        }
    }
}
