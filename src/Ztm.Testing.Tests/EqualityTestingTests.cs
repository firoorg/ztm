using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Subject=Ztm.Testing.EqualityTesting;

namespace Ztm.Testing.Tests
{
    public sealed class EqualityTestingTests
    {
        [Fact]
        public void TestEquals_WithNullSubject_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "subject",
                () => Subject.TestEquals(null, (object s) => new object()));

            Assert.Throws<ArgumentNullException>(
                "subject",
                () => Subject.TestEquals((object)null, Enumerable.Empty<Func<object, object>>()));

            Assert.Throws<ArgumentNullException>(
                "subject",
                () => Subject.TestEquals(null, Enumerable.Empty<object>()));
        }

        [Fact]
        public void TestEquals_WithNullEquals_ShouldThrow()
        {
            var subject = new object();

            Assert.Throws<ArgumentNullException>(
                "equals",
                () => Subject.TestEquals(subject, (IEnumerable<Func<object, object>>)null));

            Assert.Throws<ArgumentNullException>(
                "equals",
                () => Subject.TestEquals(subject, (IEnumerable<object>)null));
        }

        [Fact]
        public void TestEquals_WithNonEmptyEquals_ShouldReturnEqualityResultForEachItem()
        {
            var result1 = Subject.TestEquals(1, s => 1, s => 2);
            var result2 = Subject.TestEquals(1, new[] { 1, 2 });

            Assert.Collection(result1, r => Assert.True(r), r => Assert.False(r));
            Assert.Collection(result2, r => Assert.True(r), r => Assert.False(r));
        }

        [Fact]
        public void TestInequal_WithNullSubject_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "subject",
                () => Subject.TestInequal(null, (object s) => new object()));

            Assert.Throws<ArgumentNullException>(
                "subject",
                () => Subject.TestInequal((object)null, Enumerable.Empty<Func<object, object>>()));

            Assert.Throws<ArgumentNullException>(
                "subject",
                () => Subject.TestInequal(null, Enumerable.Empty<object>()));
        }

        [Fact]
        public void TestInequal_WithNullUnequals_ShouldThrow()
        {
            var subject = new object();

            Assert.Throws<ArgumentNullException>(
                "unequals",
                () => Subject.TestInequal(subject, (IEnumerable<Func<object, object>>)null));

            Assert.Throws<ArgumentNullException>(
                "unequals",
                () => Subject.TestInequal(subject, (IEnumerable<object>)null));
        }

        [Fact]
        public void TestInequal_WithNonEmptyUnequals_ShouldReturnInequalityResultForEachItem()
        {
            var result1 = Subject.TestInequal(1, s => 1, s => 2);
            var result2 = Subject.TestInequal(1, new[] { 1, 2 });

            Assert.Collection(
                result1,
                r => Assert.False(r),
                r => Assert.False(r),
                r => Assert.True(r),
                r => Assert.False(r));

            Assert.Collection(
                result2,
                r => Assert.False(r),
                r => Assert.False(r),
                r => Assert.True(r),
                r => Assert.False(r));
        }
    }
}
