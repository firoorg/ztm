using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using NSubstitute;
using Xunit;
using Ztm.Testing;
using Ztm.Zcoin.NBitcoin.Exodus;
using Ztm.Zcoin.NBitcoin.Exodus.TransactionRetrievers;

namespace Ztm.Zcoin.NBitcoin.Tests.Exodus.TransactionRetrievers
{
    public sealed class TransactionRetrieverTests
    {
        readonly int supportedId;
        readonly IExodusTransactionRetriever defaultRetriever;
        readonly TransactionRetriever subject;

        public TransactionRetrieverTests()
        {
            this.supportedId = 1;
            this.defaultRetriever = Substitute.For<IExodusTransactionRetriever>();
            this.defaultRetriever.SupportedId.Returns(this.supportedId);

            var transactionRetrievers = new Collection<IExodusTransactionRetriever>
            {
                this.defaultRetriever,
            };

            this.subject = new TransactionRetriever(transactionRetrievers);
        }

        [Fact]
        public void Construct_WithNullArgs_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "transactionRetrievers",
                () => new TransactionRetriever(null)
            );
        }

        [Fact]
        public async Task GetBalanceChangesAsync_WithNullTransaction_ShouldThrow()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                "transaction",
                () => this.subject.GetBalanceChangesAsync(null, CancellationToken.None)
            );
        }

        [Fact]
        public async Task GetBalanceChangesAsync_WithoutExodusData_ShouldRetrieveNull()
        {
            var tx =  Transaction.Parse(
                ZcoinTransactionData.ZerocoinRemint, ZcoinNetworks.Instance.Regtest);

            Assert.Null(await this.subject.GetBalanceChangesAsync(tx, CancellationToken.None));
        }

        [Fact]
        public async Task GetBalanceChangesAsync_WithUnsupportedExodusTransactionType_ShouldThrow()
        {
            var tx =  Transaction.Parse(
                ZcoinTransactionData.ZerocoinRemint, ZcoinNetworks.Instance.Regtest);
            var upsupportedId = int.MaxValue;
            var unsupported = new FakeExodusTransaction(TestAddress.Regtest1, TestAddress.Regtest2, upsupportedId, 1);

            #pragma warning disable CS0618
            tx.SetExodusTransaction(unsupported);
            #pragma warning restore CS0618

            await Assert.ThrowsAsync<TransactionException>(
                () => this.subject.GetBalanceChangesAsync(tx, CancellationToken.None)
            );
        }

        [Fact]
        public async Task GetBalanceChangesAsync_WithSupportedType_ShouldSuccess()
        {
            // Arrange.
            var tx =  Transaction.Parse(
                ZcoinTransactionData.ZerocoinRemint, ZcoinNetworks.Instance.Regtest);
            var exodus = new FakeExodusTransaction(TestAddress.Regtest1, TestAddress.Regtest2, this.supportedId, 0);

            #pragma warning disable CS0618
            tx.SetExodusTransaction(exodus);
            #pragma warning restore CS0618

            var address = TestAddress.Mainnet1;
            var amount = new PropertyAmount(10);
            var property = new PropertyId(2);

            var changes = new Collection<BalanceChange>
            {
                new BalanceChange(address, amount, property)
            };

            var cancellationToken = new CancellationToken(false);

            this.defaultRetriever.GetBalanceChangesAsync
            (
                Arg.Is<ExodusTransaction>(t => exodus == t),
                Arg.Is<CancellationToken>(c => c == cancellationToken)
            ).Returns(changes);

            // Act.
            var retrievedChanges = await this.subject.GetBalanceChangesAsync(tx, cancellationToken);

            // Assert.
            _ = this.defaultRetriever.Received(1).GetBalanceChangesAsync
            (
                Arg.Is<ExodusTransaction>(t => exodus == t),
                Arg.Is<CancellationToken>(c => c == cancellationToken)
            );

            Assert.Equal(changes, retrievedChanges);
        }
    }
}