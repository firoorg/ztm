using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;

namespace Ztm.Hosting.Tests
{
    public sealed class FakeBackgroundService : BackgroundService
    {
        public FakeBackgroundService(IBackgroundServiceExceptionHandler exceptionHandler) : base(exceptionHandler)
        {
            StubbedDispose = new Mock<Action<bool>>();
            StubbedExecuteAsync = new Mock<Func<CancellationToken, Task>>();
        }

        public Mock<Action<bool>> StubbedDispose { get; }

        public Mock<Func<CancellationToken, Task>> StubbedExecuteAsync { get; }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            StubbedDispose.Object(disposing);
        }

        protected override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            return StubbedExecuteAsync.Object(cancellationToken);
        }
    }
}
