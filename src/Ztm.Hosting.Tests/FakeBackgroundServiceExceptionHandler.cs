using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;

namespace Ztm.Hosting.Tests
{
    sealed class FakeBackgroundServiceExceptionHandler : BackgroundServiceExceptionHandler
    {
        public FakeBackgroundServiceExceptionHandler()
        {
            StubbedRunAsync = new Mock<Func<Type, Exception, CancellationToken, Task>>();
        }

        public Mock<Func<Type, Exception, CancellationToken, Task>> StubbedRunAsync { get; }

        protected override Task RunAsync(Type service, Exception exception, CancellationToken cancellationToken)
        {
            return StubbedRunAsync.Object(service, exception, cancellationToken);
        }
    }
}
