using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.Zcoin.Rpc.Tests
{
    sealed class PropertyIssuer
    {
        readonly RpcFactory factory;

        public PropertyIssuer(RpcFactory factory)
        {
            this.factory = factory;

            Ecosystem = Ecosystem.Main;
            Type = PropertyType.Indivisible;
            Category = "Company";
            SubCategory = "Private";
            Name = "Satang Corporation";
            Description = "Provides cryptocurrency solutions.";
            Url = "https://satang.com";
        }

        public string Category { get; set; }
        public Property Current { get; set; }
        public string Description { get; set; }
        public Ecosystem Ecosystem { get; set; }
        public string Name { get; set; }
        public string SubCategory { get; set; }
        public PropertyType Type { get; set; }
        public string Url { get; set; }

        public async Task<Transaction> CreateManagedIssuingTransactionAsync(BitcoinAddress owner)
        {
            using (var rpc = await this.factory.CreatePropertyManagementRpcAsync(CancellationToken.None))
            {
                return await rpc.CreateManagedAsync(
                    owner,
                    Ecosystem,
                    Type,
                    Current,
                    Category,
                    SubCategory,
                    Name,
                    Url,
                    Description,
                    CancellationToken.None
                );
            }
        }

        public async Task<Transaction> IssueManagedAsync(BitcoinAddress owner)
        {
            var tx = await CreateManagedIssuingTransactionAsync(owner);

            using (var rpc = await this.factory.CreateRawTransactionRpcAsync(CancellationToken.None))
            {
                await rpc.SendAsync(tx, CancellationToken.None);
            }

            return tx;
        }
    }
}
