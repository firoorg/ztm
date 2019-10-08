using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ztm.Threading.Tests
{
    public class ShutdownGuardTests : IDisposable
    {
        readonly Thread thread1, thread2, thread3, thread4;
        readonly ShutdownGuard subject;

        public ShutdownGuardTests()
        {
            this.thread1 = new Thread(SeparatedThread);
            this.thread2 = new Thread(SeparatedThread);
            this.thread3 = new Thread(SeparatedThread);
            this.thread4 = new Thread(SeparatedThread);
            this.subject = new ShutdownGuard();
        }

        public void Dispose()
        {
            ShutdownThread(this.thread1);
            ShutdownThread(this.thread2);
            ShutdownThread(this.thread3);
            ShutdownThread(this.thread4);

            this.subject.Dispose();
        }

        [Fact]
        public async Task SetAndWaitAsync_WhileOtherThreadRepeatedTryLock_ShouldSuccess()
        {
            // Arrange.
            this.thread1.Start();
            this.thread2.Start();
            this.thread3.Start();
            this.thread4.Start();

            await Task.Delay(500);

            Assert.True(this.thread1.IsAlive);
            Assert.True(this.thread2.IsAlive);
            Assert.True(this.thread3.IsAlive);
            Assert.True(this.thread4.IsAlive);

            // Act.
            await this.subject.SetAndWaitAsync(CancellationToken.None);
            await Task.Delay(500);

            // Assert.
            Assert.False(this.thread1.IsAlive);
            Assert.False(this.thread2.IsAlive);
            Assert.False(this.thread3.IsAlive);
            Assert.False(this.thread4.IsAlive);
        }

        void SeparatedThread()
        {
            while (true)
            {
                if (!this.subject.TryLock())
                {
                    break;
                }

                this.subject.Release();
            }
        }

        void ShutdownThread(Thread thread)
        {
            while (true)
            {
                try
                {
                    thread.Join();
                }
                catch (ThreadStateException)
                {
                    break;
                }
                catch (ThreadInterruptedException)
                {
                    continue;
                }

                break;
            }
        }
    }
}
