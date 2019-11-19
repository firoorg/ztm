using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using NBitcoin.RPC;
using Newtonsoft.Json.Linq;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.Zcoin.Rpc
{
    public sealed class ZcoinRpcClient : IZcoinRpcClient
    {
        readonly RPCClient client;

        public ZcoinRpcClient(RPCClient client)
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

        public async Task<Transaction> CreateManagedPropertyAsync(
            BitcoinAddress owner,
            Ecosystem ecosystem,
            PropertyType type,
            Property current,
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
                ToNative(ecosystem),
                ToNative(type),
                current != null ? ToNative(current.Id) : 0U,
                category,
                subcategory,
                name,
                url,
                description
            );

            return Transaction.Parse(resp.Result.Value<string>(), this.client.Network);
        }

        public Task<Block> GetBlockAsync(uint256 blockHash, CancellationToken cancellationToken)
        {
            return this.client.GetBlockAsync(blockHash);
        }

        public Task<Block> GetBlockAsync(int height, CancellationToken cancellationToken)
        {
            return this.client.GetBlockAsync(height);
        }

        public Task<BlockHeader> GetBlockHeaderAsync(uint256 blockHash, CancellationToken cancellationToken)
        {
            return this.client.GetBlockHeaderAsync(blockHash);
        }

        public Task<BlockHeader> GetBlockHeaderAsync(int height, CancellationToken cancellationToken)
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

        public async Task<(PropertyAmount Balance, PropertyAmount Reserved)> GetPropertyBalanceAsync(
            BitcoinAddress address,
            Property property,
            CancellationToken cancellationToken)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            var resp = await this.client.SendCommandAsync("exodus_getbalance", address.ToString(), ToNative(property.Id));
            var rawBalance = resp.Result.Value<string>("balance");
            var rawReserved = resp.Result.Value<string>("reserved");

            var balance = property.Type == PropertyType.Divisible
                ? PropertyAmount.FromDivisible(Convert.ToDecimal(rawBalance))
                : new PropertyAmount(Convert.ToInt64(rawBalance));

            var reserved = property.Type == PropertyType.Divisible
                ? PropertyAmount.FromDivisible(Convert.ToDecimal(rawReserved))
                : new PropertyAmount(Convert.ToInt64(rawReserved));

            return (balance, reserved);
        }

        public async Task<PropertyGrantsInfo> GetPropertyGrantsAsync(
            Property property,
            CancellationToken cancellationToken)
        {
            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            var resp = await this.client.SendCommandAsync("exodus_getgrants", ToNative(property.Id));

            return new PropertyGrantsInfo()
            {
                Id = new PropertyId(resp.Result.Value<long>("propertyid")),
                Name = resp.Result.Value<string>("name"),
                Issuer = BitcoinAddress.Create(resp.Result.Value<string>("issuer"), this.client.Network),
                CreationTransaction = uint256.Parse(resp.Result.Value<string>("creationtxid")),
                TotalTokens = PropertyAmount.Parse(resp.Result.Value<string>("totaltokens")),
                Histories = ((JArray)resp.Result["issuances"]).Select(i =>
                {
                    var grant = i.Value<string>("grant");
                    var revoke = i.Value<string>("revoke");

                    return new PropertyGrantHistory()
                    {
                        Type = (grant != null) ? PropertyGrantType.Grant : PropertyGrantType.Revoke,
                        Transaction = uint256.Parse(i.Value<string>("txid")),
                        Amount = PropertyAmount.Parse(grant ?? revoke)
                    };
                }).ToArray()
            };
        }

        public async Task<Transaction> GrantPropertyAsync(
            Property property,
            BitcoinAddress from,
            BitcoinAddress to,
            PropertyAmount amount,
            string note,
            CancellationToken cancellationToken)
        {
            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            if (from == null)
            {
                throw new ArgumentNullException(nameof(from));
            }

            if (to == null)
            {
                throw new ArgumentNullException(nameof(to));
            }

            if (amount < PropertyAmount.One)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "The value is less than one.");
            }

            // Setup arguments.
            var args = new List<object>()
            {
                from.ToString(),
                to.ToString(),
                ToNative(property.Id),
                amount.ToString(property.Type)
            };

            if (note != null)
            {
                args.Add(note);
            }

            // Invoke RPC.
            var resp = await this.client.SendCommandAsync("exodus_sendgrant", args.ToArray());

            return Transaction.Parse(resp.Result.Value<string>(), this.client.Network);
        }

        public async Task<IEnumerable<PropertyInfo>> ListPropertiesAsync(CancellationToken cancellationToken)
        {
            var resp = await this.client.SendCommandAsync("exodus_listproperties");

            return ((JArray)resp.Result).Select(i => new PropertyInfo()
            {
                Id = new PropertyId(i.Value<long>("propertyid")),
                Name = i.Value<string>("name"),
                Category = i.Value<string>("category"),
                Subcategory = i.Value<string>("subcategory"),
                Url = i.Value<string>("url"),
                Description = i.Value<string>("data"),
                Type = i.Value<bool>("divisible") ? PropertyType.Divisible : PropertyType.Indivisible
            }).ToArray();
        }

        public Task<uint256> SendRawTransactionAsync(Transaction tx, CancellationToken cancellationToken)
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

        public async Task<Transaction> SendAsync(
            BitcoinAddress from,
            BitcoinAddress to,
            Property property,
            PropertyAmount amount,
            BitcoinAddress redeemAddress,
            Money referenceAmount,
            CancellationToken cancellation)
        {
            if (from == null)
            {
                throw new ArgumentNullException(nameof(from));
            }

            if (to == null)
            {
                throw new ArgumentNullException(nameof(to));
            }

            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            if (amount < PropertyAmount.One)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "The value is less than one.");
            }

            var args = new List<object>()
            {
                from.ToString(),
                to.ToString(),
                ToNative(property.Id),
                amount.ToString(property.Type),
            };

            if (redeemAddress != null)
            {
                args.Add(redeemAddress.ToString());

                if (referenceAmount != null)
                {
                    if (referenceAmount < Money.Zero)
                    {
                        throw new ArgumentOutOfRangeException(nameof(referenceAmount), amount, "The value is less than zero.");
                    }
                    args.Add(referenceAmount.ToDecimal(MoneyUnit.BTC).ToString());
                }
            }

            // Invoke RPC.
            var resp = await this.client.SendCommandAsync("exodus_send", args.ToArray());

            return Transaction.Parse(resp.Result.Value<string>(), this.client.Network);
        }

        static byte ToNative(Ecosystem ecosystem)
        {
            switch (ecosystem)
            {
                case Ecosystem.Main:
                    return 1;
                case Ecosystem.Test:
                    return 2;
                default:
                    throw new ArgumentOutOfRangeException(nameof(ecosystem), ecosystem, "The value is not valid.");
            }
        }

        static uint ToNative(PropertyId id)
        {
            return Convert.ToUInt32(id.Value); // Don't use cast due to it will not check overflow.
        }

        static ushort ToNative(PropertyType type)
        {
            switch (type)
            {
                case PropertyType.Indivisible:
                    return 1;
                case PropertyType.Divisible:
                    return 2;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, "The value is not valid.");
            }
        }
    }
}
