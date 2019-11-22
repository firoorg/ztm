using System;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;

namespace Ztm.Hosting.Tests
{
    public sealed class FakeBackgroundService : BackgroundService
    {
        public FakeBackgroundService(IBackgroundServiceExceptionHandler exceptionHandler) : base(exceptionHandler)
        {
            StubbedDispose = Substitute.For<Action<bool>>();
            StubbedExecuteAsync = Substitute.For<Func<CancellationToken, Task>>();
            StubbedExecuteAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        }

        public Action<bool> StubbedDispose { get; }

        public Func<CancellationToken, Task> StubbedExecuteAsync { get; }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            StubbedDispose(disposing);
        }

        protected override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            return StubbedExecuteAsync(cancellationToken);
        }
    }
}
