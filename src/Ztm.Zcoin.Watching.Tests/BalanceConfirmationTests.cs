using System;
using System.Collections.ObjectModel;
using System.Linq;
using Xunit;
using Ztm.Testing;

namespace Ztm.Zcoin.Watching.Tests
{
    public sealed class BalanceConfirmationTests
    {
        readonly Collection<ConfirmedBalanceChange<object, int>> changes;
        readonly BalanceConfirmation<object, int> subject;

        public BalanceConfirmationTests()
        {
            this.changes = new Collection<ConfirmedBalanceChange<object, int>>()
            {
                new ConfirmedBalanceChange<object, int>(new object(), 1, 1)
            };
            this.subject = new BalanceConfirmation<object, int>(TestAddress.Regtest1, this.changes);
        }

        [Fact]
        public void Constructor_WithNullAddress_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "address",
                () => new BalanceConfirmation<object, int>(null, this.changes)
            );
        }

        [Fact]
        public void Constructor_WithNullChanges_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "changes",
                () => new BalanceConfirmation<object, int>(TestAddress.Regtest1, null)
            );
        }

        [Fact]
        public void Constructor_WithEmptyChanges_ShouldThrow()
        {
            Assert.Throws<ArgumentException>(
                "changes",
                () => new BalanceConfirmation<object, int>(
                    TestAddress.Regtest1,
                    Enumerable.Empty<ConfirmedBalanceChange<object, int>>()
                )
            );
        }

        [Fact]
        public void Constructor_WhenSuccess_ShouldInitializeProperties()
        {
            Assert.Equal(TestAddress.Regtest1, this.subject.Address);
            Assert.Equal(this.changes, this.subject.Changes);
        }
    }
}
