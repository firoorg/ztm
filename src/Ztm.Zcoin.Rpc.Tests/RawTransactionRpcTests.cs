using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using Xunit;

namespace Ztm.Zcoin.Rpc.Tests
{
    public sealed class RawTransactionRpcTests : RpcClientTesting
    {
        new RawTransactionRpc Subject => (RawTransactionRpc)base.Subject;

        [Fact]
        public Task SendAsync_WithNullTx_ShouldThrow()
        {
            return Assert.ThrowsAsync<ArgumentNullException>(
                "tx",
                () => Subject.SendAsync(null, CancellationToken.None)
            );
        }

        [Fact]
        public async Task SendAsync_WithValidTx_ShouldGetMined()
        {
            // Arrange.
            var owner = await GenerateNewAddressAsync();
            var issuer = new PropertyIssuer(Factory);

            Node.Generate(101);
            await FundAddressAsync(owner, Money.Coins(1));
            Node.Generate(1);

            var tx = await issuer.CreateManagedIssuingTransactionAsync(owner);

            // Act.
            var hash = await Subject.SendAsync(tx, CancellationToken.None);
            var mined = Node.Generate(1).Single();

            // Assert.
            Assert.Equal(tx.GetHash(), hash);

            using (var rpc = await Factory.CreateChainInformationRpcAsync(CancellationToken.None))
            {
                var block = await rpc.GetBlockAsync(mined, CancellationToken.None);

                Assert.Contains(block.Transactions, t => t.GetHash() == hash);
            }
        }

        protected override RpcClient CreateSubject()
        {
            return new RawTransactionRpc(Factory, Node.CreateRPCClient());
        }
    }
}
