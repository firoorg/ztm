using System.Threading;
using Xunit;

namespace Ztm.Threading.Tests
{
    public class TimerElapsedEventArgsTests
    {
        [Fact]
        public void Constructor_WithContext_ShouldInitializeContextPropertyWithThatValue()
        {
            var context = new object();
            var subject = new TimerElapsedEventArgs(context, CancellationToken.None);

            Assert.Same(context, subject.Context);
        }
    }
}
