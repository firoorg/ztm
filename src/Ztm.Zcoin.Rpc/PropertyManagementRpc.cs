using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using NBitcoin.RPC;
using Newtonsoft.Json.Linq;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.Zcoin.Rpc
{
    public sealed class PropertyManagementRpc : RpcClient, IPropertyManagementRpc
    {
        public PropertyManagementRpc(RpcFactory factory, RPCClient client) : base(factory, client)
        {
        }

        public async Task<Transaction> CreateManagedAsync(
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

            var resp = await Client.SendCommandAsync(
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

            return Transaction.Parse(resp.Result.Value<string>(), Factory.Network);
        }

        public async Task<Transaction> GrantAsync(
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
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "The value is not valid.");
            }

            // Setup arguments.
            var args = new Collection<object>()
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
            var resp = await Client.SendCommandAsync("exodus_sendgrant", args.ToArray());

            return Transaction.Parse(resp.Result.Value<string>(), Factory.Network);
        }

        public async Task<Transaction> SendAsync(
            BitcoinAddress from,
            BitcoinAddress to,
            Property property,
            PropertyAmount amount,
            Money referenceAmount,
            CancellationToken cancellationToken)
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
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "The value is not valid.");
            }

            // Setup arguments.
            var args = new Collection<object>()
            {
                from.ToString(),
                to.ToString(),
                ToNative(property.Id),
                amount.ToString(property.Type),
            };

            if (referenceAmount != null)
            {
                if (referenceAmount <= Money.Zero)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(referenceAmount),
                        referenceAmount,
                        "The value is not valid."
                    );
                }

                args.Add(from.ToString());
                args.Add(referenceAmount.ToDecimal(MoneyUnit.BTC).ToString());
            }

            // Invoke RPC.
            var resp = await Client.SendCommandAsync("exodus_send", args.ToArray());

            return Transaction.Parse(resp.Result.Value<string>(), Factory.Network);
        }
    }
}
