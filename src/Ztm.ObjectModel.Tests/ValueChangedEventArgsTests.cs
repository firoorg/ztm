using Xunit;

namespace Ztm.ObjectModel.Tests
{
    public class ValueChangedEventArgsTests
    {
        readonly ValueChangedEventArgs<int> subject;

        public ValueChangedEventArgsTests()
        {
            this.subject = new ValueChangedEventArgs<int>(1, 0);
        }

        [Fact]
        public void Constructor_WhenSuccess_ShouldInitProperties()
        {
            Assert.Equal(1, this.subject.Current);
            Assert.Equal(0, this.subject.Previous);
        }
    }
}
