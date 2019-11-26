using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using NBitcoin.DataEncoders;
using NBitcoin.RPC;
using Newtonsoft.Json.Linq;
using Ztm.Zcoin.NBitcoin;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.Zcoin.Rpc
{
    public sealed class ZcoinRpcClient : IZcoinRpcClient
    {
        readonly RPCClient client;
        readonly ITransactionEncoder encoder;

        public ZcoinRpcClient(RPCClient client, ITransactionEncoder encoder)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            if (encoder == null)
            {
                throw new ArgumentNullException(nameof(encoder));
            }

            this.client = client;
            this.encoder = encoder;
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

        public async Task<Block> GetBlockAsync(uint256 blockHash, CancellationToken cancellationToken)
        {
            var block = await this.client.GetBlockAsync(blockHash);

            foreach (var tx in block.Transactions)
            {
                await TryDecodeExodusTransactionAndSetAsync(tx);
            }

            return block;
        }

        public async Task<Block> GetBlockAsync(int height, CancellationToken cancellationToken)
        {
            var block = await this.client.GetBlockAsync(height);

            foreach (var tx in block.Transactions)
            {
                await TryDecodeExodusTransactionAndSetAsync(tx);
            }

            return block;
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

        public async Task<ExodusTransactionInformation> GetExodusTransactionAsync(uint256 transaction, CancellationToken cancellationToken)
        {
            // Invoke RPC.
            var resp = await this.client.SendCommandAsync("exodus_gettransaction", transaction);
            var rawRefAddress = resp.Result.Value<string>("referenceaddress");

            return new ExodusTransactionInformation
            {
                TxId = uint256.Parse(resp.Result.Value<string>("txid")),
                SendingAddress = BitcoinAddress.Create(resp.Result.Value<string>("sendingaddress"), this.client.Network),
                ReferenceAddress = string.IsNullOrEmpty(rawRefAddress)
                    ? null : BitcoinAddress.Create(rawRefAddress, this.client.Network),
                IsMine = resp.Result.Value<bool>("ismine"),
                Confirmations = resp.Result.Value<int>("confirmations"),
                Fee = Money.Parse(resp.Result.Value<string>("fee")),
                Block = resp.Result.Value<int>("block"),
                BlockHash = uint256.Parse(resp.Result.Value<string>("blockhash")),
                BlockTime = Utils.UnixTimeToDateTime(resp.Result.Value<long>("blocktime")),
                Valid = resp.Result.Value<bool>("valid"),
                InvalidReason = resp.Result.Value<string>("invalidreason"),
                Version = resp.Result.Value<int>("version"),
                TypeInt = resp.Result.Value<int>("type_int"),
                Type = resp.Result.Value<string>("type")
            };
        }

        public Task<BitcoinAddress> GetNewAddressAsync(CancellationToken cancellationToken)
        {
            return this.client.GetNewAddressAsync();
        }

        public async Task<(PropertyAmount balance, PropertyAmount reserved)> GetPropertyBalanceAsync(
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

            var balance = PropertyAmount.Parse(rawBalance);
            var reserved = PropertyAmount.Parse(rawReserved);

            return (balance: balance, reserved: reserved);
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

        public async Task<Transaction> SendTokenAsync(
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
                    if (referenceAmount < Money.Satoshis(1))
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

        async Task<(string payload, int payloadSize)> GetPayloadAsync(Transaction transaction)
        {
            var resp = await this.client.SendCommandAsync("exodus_getpayload", new object[]{transaction.GetHash()});

            var payload = resp.Result.Value<string>("payload");
            var payloadSize = resp.Result.Value<int>("payloadsize");

            return (payload: payload, payloadSize: payloadSize);
        }

        async Task<ExodusTransaction> TryDecodeExodusTransaction(Transaction transaction)
        {
            var infomation = await GetExodusTransactionAsync(transaction.GetHash(), CancellationToken.None);
            if (infomation == null)
            {
                return null;
            }

            var payload = await GetPayloadAsync(transaction);

            try
            {
                return this.encoder.Decode(infomation.SendingAddress, infomation.ReferenceAddress, Encoders.Hex.DecodeData(payload.payload));
            }
            catch (TransactionFieldException)
            {
                return null;
            }
        }

        async Task TryDecodeExodusTransactionAndSetAsync(Transaction transaction)
        {
            var exodusTransaction = await TryDecodeExodusTransaction(transaction);
            if (exodusTransaction != null)
            {
                #pragma warning disable CS0618
                transaction.SetExodusTransaction(exodusTransaction); // lgtm[cs/call-to-obsolete-method]
                #pragma warning restore CS0618
            }
        }
    }
}
