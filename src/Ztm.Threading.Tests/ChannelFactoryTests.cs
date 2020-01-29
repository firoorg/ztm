using Xunit;

namespace Ztm.Threading.Tests
{
    public sealed class ChannelFactoryTests
    {
        readonly ChannelFactory subject;

        public ChannelFactoryTests()
        {
            this.subject = new ChannelFactory();
        }

        [Fact]
        public void Create_WhenInvoke_ShouldReturnNonNull()
        {
            var result = this.subject.Create<object>();

            Assert.NotNull(result);
        }
    }
}
