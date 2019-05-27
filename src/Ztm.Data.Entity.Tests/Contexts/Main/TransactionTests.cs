using Xunit;
using Ztm.Data.Entity.Contexts.Main;

namespace Ztm.Data.Entity.Tests.Contexts.Main
{
    public class TransactionTests
    {
        readonly Transaction subject;

        public TransactionTests()
        {
            this.subject = new Transaction();
        }

        [Fact]
        public void Constructor_WhenSuccess_ShouldInitBlocksWithEmpty()
        {
            Assert.NotNull(this.subject.Blocks);
            Assert.Empty(this.subject.Blocks);
        }

        [Fact]
        public void Constructor_WhenSuccess_ShouldInitInputsWithEmpty()
        {
            Assert.NotNull(this.subject.Inputs);
            Assert.Empty(this.subject.Inputs);
        }

        [Fact]
        public void Constructor_WhenSuccess_ShouldInitOutputsWithEmpty()
        {
            Assert.NotNull(this.subject.Outputs);
            Assert.Empty(this.subject.Outputs);
        }
    }
}
