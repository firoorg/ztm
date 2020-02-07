using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Ztm.Testing.Tests
{
    public sealed class EnumMemberDataAttributeTests
    {
        readonly EnumMemberDataAttribute subject;

        public EnumMemberDataAttributeTests()
        {
            this.subject = new EnumMemberDataAttribute(
                typeof(TaskStatus),
                TaskStatus.WaitingForActivation,
                TaskStatus.Running);
        }

        [Fact]
        public void Constructor_WithNullType_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>("type", () => new EnumMemberDataAttribute(null));
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(string))]
        [InlineData(typeof(double))]
        [InlineData(typeof(object))]
        public void Constructor_WithNonEnumType_ShouldThrow(Type type)
        {
            Assert.Throws<ArgumentException>("type", () => new EnumMemberDataAttribute(type));
        }

        [Fact]
        public void Constructor_WhenSucceeded_ShouldInitializeProperties()
        {
            Assert.Equal(2, this.subject.Exclude.Count);
            Assert.Contains(TaskStatus.WaitingForActivation, this.subject.Exclude);
            Assert.Contains(TaskStatus.Running, this.subject.Exclude);
            Assert.Equal(typeof(TaskStatus), this.subject.Type);
        }

        [Fact]
        public void GetData_WhenInvoke_ShouldReturnNonExcludedValues()
        {
            // Act.
            var result = this.subject.GetData(GetType().GetMethods().First());

            // Assert.
            var expected = Enum.GetValues(typeof(TaskStatus))
                .Cast<TaskStatus>()
                .Where(s => s != TaskStatus.WaitingForActivation && s != TaskStatus.Running)
                .Cast<object>()
                .ToList();

            Assert.All(result, r => Assert.Single(r));
            Assert.Equal(expected, result.Select(r => r[0]));
        }
    }
}
