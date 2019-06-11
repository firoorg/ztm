using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using NBitcoin.RPC;
using Newtonsoft.Json.Linq;
using Ztm.Zcoin.NBitcoin;

namespace Ztm.Zcoin.Rpc
{
    public sealed class ZcoinRpcClient : IZcoinRpcClient
    {
        readonly Ztm.Zcoin.NBitcoin.RPC.ZcoinRPCClient client;

        public ZcoinRpcClient(Ztm.Zcoin.NBitcoin.RPC.ZcoinRPCClient client)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            this.client = client;
        }

        public void Dispose()
        {
        }

        public async Task<ZcoinTransaction> CreateManagedTokenAsync(
            BitcoinAddress owner,
            TokenEcosystem ecosystem,
            TokenType type,
            TokenId? currentId,
            string category,
            string subcategory,
            string name,
            string url,
            string description,
            CancellationToken cancellationToken)
        {
            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner));
            }

            if (currentId.HasValue && !currentId.Value.IsValid)
            {
                throw new ArgumentException("The value is not valid token identifier.", nameof(currentId));
            }

            if (category == null)
            {
                throw new ArgumentNullException(nameof(category));
            }

            if (subcategory == null)
            {
                throw new ArgumentNullException(nameof(subcategory));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (url == null)
            {
                throw new ArgumentNullException(nameof(url));
            }

            if (description == null)
            {
                throw new ArgumentNullException(nameof(description));
            }

            // Send request.
            var resp = await this.client.SendCommandAsync(
                "exodus_sendissuancemanaged",
                owner.ToString(),
                (byte)ecosystem,
                (ushort)type,
                currentId.HasValue ? currentId.Value.Value : 0,
                category,
                subcategory,
                name,
                url,
                description
            );

            return ZcoinTransaction.Parse(resp.Result.Value<string>(), this.client.Network);
        }

        public Task<ZcoinBlock> GetBlockAsync(uint256 blockHash, CancellationToken cancellationToken)
        {
            return this.client.GetBlockAsync(blockHash);
        }

        public Task<ZcoinBlock> GetBlockAsync(int height, CancellationToken cancellationToken)
        {
            return this.client.GetBlockAsync(height);
        }

        public Task<ZcoinBlockHeader> GetBlockHeaderAsync(uint256 blockHash, CancellationToken cancellationToken)
        {
            return this.client.GetBlockHeaderAsync(blockHash);
        }

        public Task<ZcoinBlockHeader> GetBlockHeaderAsync(int height, CancellationToken cancellationToken)
        {
            return this.client.GetBlockHeaderAsync(height);
        }

        public Task<BlockchainInfo> GetBlockchainInfoAsync(CancellationToken cancellationToken)
        {
            return this.client.GetBlockchainInfoAsync();
        }

        public Task<BitcoinAddress> GetNewAddressAsync(CancellationToken cancellationToken)
        {
            return this.client.GetNewAddressAsync();
        }

        public async Task<TokenGrantsInfo> GetTokenGrantsAsync(TokenId token, CancellationToken cancellationToken)
        {
            if (!token.IsValid)
            {
                throw new ArgumentException("The value is not valid token identifier.", nameof(token));
            }

            var resp = await this.client.SendCommandAsync("exodus_getgrants", token.Value);
            var totalTokens = TokenAmount.Parse(resp.Result.Value<string>("totaltokens"));

            return new TokenGrantsInfo()
            {
                Id = resp.Result.Value<long>("propertyid"),
                Name = resp.Result.Value<string>("name"),
                Issuer = BitcoinAddress.Create(resp.Result.Value<string>("issuer"), this.client.Network),
                CreationTransaction = uint256.Parse(resp.Result.Value<string>("creationtxid")),
                TotalTokens = totalTokens.IsValid ? (TokenAmount?)totalTokens : null,
                Histories = ((JArray)resp.Result["issuances"]).Select(i =>
                {
                    var grant = i.Value<string>("grant");
                    var revoke = i.Value<string>("revoke");

                    return new TokenGrantHistory()
                    {
                        Type = (grant != null) ? TokenGrantType.Grant : TokenGrantType.Revoke,
                        Transaction = uint256.Parse(i.Value<string>("txid")),
                        Amount = TokenAmount.Parse(grant ?? revoke)
                    };
                }).ToArray()
            };
        }

        public async Task<ZcoinTransaction> GrantTokensAsync(
            TokenId id,
            BitcoinAddress from,
            BitcoinAddress to,
            TokenAmount amount,
            string note,
            CancellationToken cancellationToken)
        {
            if (!id.IsValid)
            {
                throw new ArgumentException("The value is not valid token identifier.", nameof(id));
            }

            if (from == null)
            {
                throw new ArgumentNullException(nameof(from));
            }

            if (to == null)
            {
                throw new ArgumentNullException(nameof(to));
            }

            if (!amount.IsValid)
            {
                throw new ArgumentException("The value is not valid token amount.", nameof(amount));
            }

            var args = new List<object>()
            {
                from.ToString(),
                to.ToString(),
                id.Value,
                amount.ToString()
            };

            if (note != null)
            {
                args.Add(note);
            }

            var resp = await this.client.SendCommandAsync("exodus_sendgrant", args.ToArray());

            return ZcoinTransaction.Parse(resp.Result.Value<string>(), this.client.Network);
        }

        public async Task<IEnumerable<TokenInfo>> ListTokensAsync(CancellationToken cancellationToken)
        {
            var resp = await this.client.SendCommandAsync("exodus_listproperties");

            return ((JArray)resp.Result).Select(i => new TokenInfo()
            {
                Id = i.Value<long>("propertyid"),
                Name = i.Value<string>("name"),
                Category = i.Value<string>("category"),
                Subcategory = i.Value<string>("subcategory"),
                Url = i.Value<string>("url"),
                Description = i.Value<string>("data"),
                Type = i.Value<bool>("divisible") ? TokenType.Divisible : TokenType.Indivisible
            }).ToArray();
        }

        public Task<uint256> SendRawTransactionAsync(ZcoinTransaction tx, CancellationToken cancellationToken)
        {
            if (tx == null)
            {
                throw new ArgumentNullException(nameof(tx));
            }

            return this.client.SendRawTransactionAsync(tx);
        }

        public Task<uint256> SendToAddressAsync(
            BitcoinAddress address,
            Money amount,
            string comment,
            string commentTo,
            bool subtractFeeFromAmount,
            CancellationToken cancellationToken)
        {
            return this.client.SendToAddressAsync(
                address,
                amount,
                comment,
                commentTo,
                subtractFeeFromAmount
            );
        }
    }
}
