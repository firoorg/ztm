using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ztm.Testing
{
    public static class AsynchronousTesting
    {
        public static void WithCancellationToken(Action<CancellationToken> test)
        {
            if (test == null)
            {
                throw new ArgumentNullException(nameof(test));
            }

            using (var source = new CancellationTokenSource())
            {
                test(source.Token);
            }
        }

        public static async Task WithCancellationTokenAsync(Func<CancellationToken, Task> test)
        {
            if (test == null)
            {
                throw new ArgumentNullException(nameof(test));
            }

            using (var source = new CancellationTokenSource())
            {
                await test(source.Token);
            }
        }

        public static async Task WithCancellationTokenAsync(
            Func<CancellationToken, Action, Task> test,
            Action<CancellationTokenSource> cancel)
        {
            if (test == null)
            {
                throw new ArgumentNullException(nameof(test));
            }

            if (cancel == null)
            {
                throw new ArgumentNullException(nameof(cancel));
            }

            using (var source = new CancellationTokenSource())
            {
                await test(source.Token, () => cancel(source));
            }
        }

        public static async Task WithCancellationTokenAsync(
            Func<CancellationToken, Func<Task>, Task> test,
            Func<CancellationTokenSource, Task> cancel)
        {
            if (test == null)
            {
                throw new ArgumentNullException(nameof(test));
            }

            if (cancel == null)
            {
                throw new ArgumentNullException(nameof(cancel));
            }

            using (var source = new CancellationTokenSource())
            {
                await test(source.Token, () => cancel(source));
            }
        }
    }
}
