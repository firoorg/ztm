using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using NBitcoin.DataEncoders;
using NBitcoin.RPC;
using Newtonsoft.Json.Linq;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.Zcoin.Rpc
{
    public sealed class ExodusInformationRpc : RpcClient, IExodusInformationRpc
    {
        public ExodusInformationRpc(RpcFactory factory, RPCClient client) : base(factory, client)
        {
        }

        public async Task<(PropertyAmount balance, PropertyAmount reserved)> GetBalanceAsync(
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

            var resp = await Client.SendCommandAsync("exodus_getbalance", address.ToString(), ToNative(property.Id));

            var balance = resp.Result.Value<string>("balance");
            var reserved = resp.Result.Value<string>("reserved");

            return (balance: PropertyAmount.Parse(balance), reserved: PropertyAmount.Parse(reserved));
        }

        public async Task<PropertyGrantsInfo> GetGrantsAsync(Property property, CancellationToken cancellationToken)
        {
            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            var resp = await Client.SendCommandAsync("exodus_getgrants", ToNative(property.Id));

            return new PropertyGrantsInfo()
            {
                Id = new PropertyId(resp.Result.Value<long>("propertyid")),
                Name = resp.Result.Value<string>("name"),
                Issuer = BitcoinAddress.Create(resp.Result.Value<string>("issuer"), Factory.Network),
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
                }).ToList()
            };
        }

        public async Task<byte[]> GetPayloadAsync(uint256 tx, CancellationToken cancellationToken)
        {
            if (tx == null)
            {
                throw new ArgumentNullException(nameof(tx));
            }

            var resp = await Client.SendCommandAsync("exodus_getpayload", tx);

            return Encoders.Hex.DecodeData(resp.Result.Value<string>("payload"));
        }

        public async Task<ExodusTransactionInformation> GetTransactionAsync(
            uint256 hash,
            CancellationToken cancellationToken)
        {
            if (hash == null)
            {
                throw new ArgumentNullException(nameof(hash));
            }

            var resp = await Client.SendCommandAsync("exodus_gettransaction", hash);

            var blockHash = resp.Result.Value<string>("blockhash");
            var blockTime = resp.Result.Value<long?>("blocktime");
            var referenceAddress = resp.Result.Value<string>("referenceaddress");

            return new ExodusTransactionInformation()
            {
                TxId = uint256.Parse(resp.Result.Value<string>("txid")),
                SendingAddress = BitcoinAddress.Create(resp.Result.Value<string>("sendingaddress"), Factory.Network),
                ReferenceAddress = string.IsNullOrEmpty(referenceAddress)
                    ? null
                    : BitcoinAddress.Create(referenceAddress, Factory.Network),
                IsMine = resp.Result.Value<bool>("ismine"),
                Confirmations = resp.Result.Value<int>("confirmations"),
                Fee = Money.Parse(resp.Result.Value<string>("fee")),
                Block = resp.Result.Value<int?>("block"),
                BlockHash = (blockHash == null) ? null : uint256.Parse(blockHash),
                BlockTime = (blockTime == null) ? null : (DateTimeOffset?)Utils.UnixTimeToDateTime(blockTime.Value),
                Valid = resp.Result.Value<bool?>("valid"),
                InvalidReason = resp.Result.Value<string>("invalidreason"),
                Version = resp.Result.Value<int>("version"),
                TypeInt = resp.Result.Value<int>("type_int"),
                Type = resp.Result.Value<string>("type")
            };
        }

        public async Task<IEnumerable<PropertyInfo>> ListPropertiesAsync(CancellationToken cancellationToken)
        {
            var resp = await Client.SendCommandAsync("exodus_listproperties");

            return ((JArray)resp.Result).Select(i => new PropertyInfo()
            {
                Id = new PropertyId(i.Value<long>("propertyid")),
                Name = i.Value<string>("name"),
                Category = i.Value<string>("category"),
                Subcategory = i.Value<string>("subcategory"),
                Url = i.Value<string>("url"),
                Description = i.Value<string>("data"),
                Type = i.Value<bool>("divisible") ? PropertyType.Divisible : PropertyType.Indivisible
            }).ToList();
        }
    }
}
