using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Ztm.Testing;
using Ztm.Zcoin.NBitcoin.Exodus;
using Ztm.Zcoin.NBitcoin.Exodus.TransactionRetrievers;

namespace Ztm.Zcoin.NBitcoin.Tests.Exodus.TransactionRetrievers
{
    public sealed class SimpleSendRetrieverTests
    {
        readonly SimpleSendRetriever subject;

        public SimpleSendRetrieverTests()
        {
            this.subject = new SimpleSendRetriever();
        }

        [Fact]
        public void SupportedId_ShouldReturnStaticIdOfSimpleSendV0()
        {
            Assert.Equal(SimpleSendV0.StaticId, this.subject.SupportedId);
        }

        [Fact]
        public async Task GetBalanceChangesAsync_WithNullExodusTransaction_ShouldThrow()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                "transaction",
                () => this.subject.GetBalanceChangesAsync(null, CancellationToken.None)
            );
        }

        [Fact]
        public async Task GetBalanceChangesAsync_WithSimepleSend_ShouldReturnBalanceChanges()
        {
            // Arrange.
            var sender = TestAddress.Regtest1;
            var receiver = TestAddress.Regtest2;
            var property = new PropertyId(2);
            var amount = new PropertyAmount(100);

            var tx = new SimpleSendV0(sender, receiver, property, amount);

            // Act.
            var changes = await this.subject.GetBalanceChangesAsync(tx, CancellationToken.None);

            // Assert.
            Assert.NotNull(changes);
            Assert.Equal(2, changes.Count());
            Assert.Contains(new BalanceChange(sender, PropertyAmount.Negate(amount), property), changes);
            Assert.Contains(new BalanceChange(receiver, amount, property), changes);
        }
    }
}