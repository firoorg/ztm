using Xunit;
using Ztm.Data.Entity.Postgres.Mapping;

namespace Ztm.Data.Entity.Postgres.Tests.Mapping
{
    public sealed class UInt256TypeMappingTests
    {
        readonly UInt256TypeMapping subject;

        public UInt256TypeMappingTests()
        {
            this.subject = new UInt256TypeMapping();
        }

        [Fact]
        public void Clone_WithStorageTypeAndSize_ShouldSuccess()
        {
            // Arrange.
            var storageType = "foo";
            var size = 10;

            // Act.
            var cloned = this.subject.Clone(storageType, size);

            // Assert.
            Assert.IsType<UInt256TypeMapping>(cloned);
            Assert.NotSame(this.subject, cloned);
        }

        [Fact]
        public void Clone_WithValueConverter_ShouldSuccess()
        {
            // Arrange.
            var converter = Converters.UInt256ToBytesConverter;

            // Act.
            var cloned = this.subject.Clone(converter);

            // Assert.
            Assert.IsType<UInt256TypeMapping>(cloned);
            Assert.NotSame(this.subject, cloned);
        }
    }
}