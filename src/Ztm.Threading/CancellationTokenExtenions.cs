using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ztm.Threading
{
    public static class CancellationTokenExtenions
    {
        public static Task WaitAsync(this CancellationToken token, CancellationToken cancellationToken)
        {
            if (!token.CanBeCanceled)
            {
                throw new InvalidOperationException("The token cannot be canceled.");
            }

            var completionSource = new TaskCompletionSource<bool>();

            token.Register(() =>
            {
                try
                {
                    completionSource.SetResult(true);
                }
                catch (InvalidOperationException)
                {
                    // Ignore.
                }
            });

            if (cancellationToken.CanBeCanceled)
            {
                cancellationToken.Register(() =>
                {
                    try
                    {
                        completionSource.SetCanceled();
                    }
                    catch (InvalidOperationException)
                    {
                        // Ignore.
                    }
                });
            }

            return completionSource.Task;
        }
    }
}
