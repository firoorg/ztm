using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Ztm.Threading
{
    public sealed class ShutdownGuard : IDisposable
    {
        readonly SemaphoreSlim releaseEvent;
        volatile bool shuttingdown;
        volatile int lockCount;
        bool disposed;

        public ShutdownGuard()
        {
            this.releaseEvent = new SemaphoreSlim(0);
        }

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            Debug.Assert(this.lockCount <= 0);

            this.releaseEvent.Dispose();

            this.disposed = true;
        }

        public int Release()
        {
            var current = Interlocked.Decrement(ref this.lockCount);

            Debug.Assert(current >= 0);

            if (this.shuttingdown && current == 0)
            {
                this.releaseEvent.Release();
            }

            return current;
        }

        public async Task SetAndWaitAsync(CancellationToken cancellationToken)
        {
            this.shuttingdown = true;

            while (Interlocked.CompareExchange(ref this.lockCount, int.MinValue, 0) != 0)
            {
                await this.releaseEvent.WaitAsync(cancellationToken);
            }
        }

        public bool TryLock()
        {
            if (this.shuttingdown)
            {
                return false;
            }

            return Interlocked.Increment(ref this.lockCount) > 0;
        }
    }
}
