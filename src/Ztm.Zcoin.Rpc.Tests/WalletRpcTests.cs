using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using Xunit;
using Ztm.Testing;

namespace Ztm.Zcoin.Rpc.Tests
{
    public sealed class WalletRpcTests : RpcClientTesting
    {
        new WalletRpc Subject => (WalletRpc)base.Subject;

        [Fact]
        public async Task GetNewAddressAsync_WhenInvoke_ShouldReturnNonNull()
        {
            var address = await Subject.GetNewAddressAsync(CancellationToken.None);

            Assert.NotNull(address);
        }

        [Fact]
        public Task SendAsync_WithNullDestination_ShouldThrow()
        {
            return Assert.ThrowsAsync<ArgumentNullException>(
                "destination",
                () => Subject.SendAsync(null, Money.Coins(1), null, null, false, CancellationToken.None)
            );
        }

        [Fact]
        public Task SendAsync_WithNullAmount_ShouldThrow()
        {
            return Assert.ThrowsAsync<ArgumentNullException>(
                "amount",
                () => Subject.SendAsync(TestAddress.Regtest1, null, null, null, false, CancellationToken.None)
            );
        }

        [Fact]
        public Task SendAsync_WithInvalidAmount_ShouldThrow()
        {
            return Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                "amount",
                () => Subject.SendAsync(TestAddress.Regtest1, Money.Zero, null, null, false, CancellationToken.None)
            );
        }

        [Fact]
        public async Task SendAsync_WithValidArgs_ShouldGetMined()
        {
            // Arrange.
            var receiver = await GenerateNewAddressAsync();

            Node.Generate(101);

            // Act.
            var tx = await Subject.SendAsync(receiver, Money.Coins(1), null, null, false, CancellationToken.None);
            var hash = Node.Generate(1).Single();

            // Assert.
            var block = await GetBlockAsync(hash);

            Assert.Contains(block.Transactions, t => t.GetHash() == tx);
        }

        protected override RpcClient CreateSubject()
        {
            return new WalletRpc(Factory, Node.CreateRPCClient());
        }
    }
}
