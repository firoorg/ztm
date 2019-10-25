using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using NSubstitute;
using Xunit;
using Ztm.Zcoin.NBitcoin;
using Ztm.Zcoin.Synchronization.Watchers;

namespace Ztm.Zcoin.Synchronization.Tests.Watchers
{
    public sealed class TransactionConfirmationWatcherTests
    {
        readonly ITransactionConfirmationWatcherHandler<object> handler;
        readonly IBlocksStorage blocks;
        readonly TransactionConfirmationWatcher<object> subject;

        public TransactionConfirmationWatcherTests()
        {
            this.handler = Substitute.For<ITransactionConfirmationWatcherHandler<object>>();
            this.blocks = Substitute.For<IBlocksStorage>();
            this.subject = new TransactionConfirmationWatcher<object>(this.handler, this.blocks);
        }

        [Fact]
        public async Task CreateWatchesAsync_CreateContextsAsyncReturnEmptyList_ShouldNotCreateAnyWatches()
        {
            // Arrange.
            var block = ZcoinNetworks.Instance.Regtest.GetGenesis();

            this.handler.CreateContextsAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>()).Returns(Enumerable.Empty<object>());
            this.handler.GetCurrentWatchesAsync(Arg.Any<CancellationToken>()).Returns(Enumerable.Empty<TransactionWatch<object>>());

            using (var cancellationSource = new CancellationTokenSource())
            {
                // Act.
                await this.subject.ExecuteAsync(block, 0, BlockEventType.Added, cancellationSource.Token);

                // Assert.
                _ = this.handler.Received(1).CreateContextsAsync(block.Transactions[0], cancellationSource.Token);
                _ = this.handler.Received(0).AddWatchesAsync(Arg.Any<IEnumerable<TransactionWatch<object>>>(), Arg.Any<CancellationToken>());
            }
        }

        [Fact]
        public async Task CreateWatchesAsync_CreateContextsAsyncReturnNonEmptyList_ShouldCreateWatchesEqualToNumberOfContexts()
        {
            // Arrange.
            var block = ZcoinNetworks.Instance.Regtest.GetGenesis();
            var ctx1 = new object();
            var ctx2 = new object();

            this.handler.CreateContextsAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>()).Returns(new[] { ctx1, ctx2 });
            this.handler.GetCurrentWatchesAsync(Arg.Any<CancellationToken>()).Returns(Enumerable.Empty<TransactionWatch<object>>());

            using (var cancellationSource = new CancellationTokenSource())
            {
                // Act.
                await this.subject.ExecuteAsync(block, 0, BlockEventType.Added, cancellationSource.Token);

                // Assert.
                _ = this.handler.Received(1).CreateContextsAsync(block.Transactions[0], cancellationSource.Token);
                _ = this.handler.Received(1).AddWatchesAsync(
                    Arg.Is<IEnumerable<TransactionWatch<object>>>(l => l.Count() == 2 && l.First().Context == ctx1 && l.Last().Context == ctx2),
                    cancellationSource.Token
                );
            }
        }
    }
}
