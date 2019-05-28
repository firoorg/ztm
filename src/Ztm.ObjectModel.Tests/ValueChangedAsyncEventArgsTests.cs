using System.Threading;
using Xunit;

namespace Ztm.ObjectModel.Tests
{
    public class ValueChangedAsyncEventArgsTests
    {
        readonly ValueChangedAsyncEventArgs<int> subject;

        public ValueChangedAsyncEventArgsTests()
        {
            this.subject = new ValueChangedAsyncEventArgs<int>(1, 0, CancellationToken.None);
        }

        [Fact]
        public void Constructor_WhenSuccess_ShouldInitProperties()
        {
            Assert.Equal(1, this.subject.Current);
            Assert.Equal(0, this.subject.Previous);
        }
    }
}
