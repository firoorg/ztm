using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Ztm.Testing
{
    public static class EqualityTesting
    {
        public static IEnumerable<bool> TestEquals<T>(T subject, params Func<T, T>[] equals)
        {
            return TestEquals(subject, equals.AsEnumerable());
        }

        public static IEnumerable<bool> TestEquals<T>(T subject, IEnumerable<Func<T, T>> equals)
        {
            if (subject == null)
            {
                throw new ArgumentNullException(nameof(subject));
            }

            if (equals == null)
            {
                throw new ArgumentNullException(nameof(equals));
            }

            var results = new Collection<bool>();
            var equatable = subject as IEquatable<T>;

            foreach (var v in equals.Select(f => f(subject)))
            {
                var inequal = false;

                inequal |= !subject.Equals(v);

                if (equatable != null)
                {
                    inequal |= !equatable.Equals(v);
                }

                results.Add(!inequal);
            }

            return results;
        }

        public static IEnumerable<bool> TestInequal<T>(T subject, params Func<T, T>[] inequals)
        {
            return TestInequal(subject, inequals.AsEnumerable());
        }

        public static IEnumerable<bool> TestInequal<T>(T subject, IEnumerable<Func<T, T>> inequals)
        {
            if (subject == null)
            {
                throw new ArgumentNullException(nameof(subject));
            }

            if (inequals == null)
            {
                throw new ArgumentNullException(nameof(inequals));
            }

            var results = new Collection<bool>();
            var equatable = subject as IEquatable<T>;

            results.Add(subject.Equals(null)); // lgtm[cs/null-argument-to-equals]
            results.Add(subject.Equals(new {}));

            foreach (var v in inequals.Select(f => f(subject)))
            {
                var equal = false;

                equal |= subject.Equals(v);

                if (equatable != null)
                {
                    equal |= equatable.Equals(v);
                }

                results.Add(equal);
            }

            return results;
        }
    }
}
