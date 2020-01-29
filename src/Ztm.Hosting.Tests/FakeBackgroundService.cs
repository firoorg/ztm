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
            StubbedPostExecuteAsync = new Mock<Func<CancellationToken, Task>>();
            StubbedPreExecuteAsync = new Mock<Func<CancellationToken, Task>>();
        }

        public Mock<Action<bool>> StubbedDispose { get; }

        public Mock<Func<CancellationToken, Task>> StubbedExecuteAsync { get; }

        public Mock<Func<CancellationToken, Task>> StubbedPostExecuteAsync { get; }

        public Mock<Func<CancellationToken, Task>> StubbedPreExecuteAsync { get; }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            StubbedDispose.Object(disposing);
        }

        protected override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            return StubbedExecuteAsync.Object(cancellationToken);
        }

        protected override Task PostExecuteAsync(CancellationToken cancellationToken)
        {
            return StubbedPostExecuteAsync.Object(cancellationToken);
        }

        protected override Task PreExecuteAsync(CancellationToken cancellationToken)
        {
            return StubbedPreExecuteAsync.Object(cancellationToken);
        }
    }
}
