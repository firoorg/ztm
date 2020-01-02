using System;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using NBitcoin.RPC;
using Ztm.Zcoin.NBitcoin;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.Zcoin.Rpc
{
    public abstract class RpcClient : IDisposable
    {
        protected RpcClient(RpcFactory factory, RPCClient client)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            Factory = factory;
            Client = client;
        }

        protected RPCClient Client { get; }

        protected RpcFactory Factory { get; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected static byte ToNative(Ecosystem ecosystem)
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

        protected static uint ToNative(PropertyId id)
        {
            if (id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            return Convert.ToUInt32(id.Value); // Don't use cast due to it will not check overflow.
        }

        protected static ushort ToNative(PropertyType type)
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

        protected virtual void Dispose(bool disposing)
        {
        }

        protected async Task PopulateExodusInformationAsync(Transaction tx, CancellationToken cancellationToken)
        {
            if (tx == null)
            {
                throw new ArgumentNullException(nameof(tx));
            }

            if (Factory.GenesisTransactions.Contains(tx.GetHash()))
            {
                return;
            }

            var exodus = await TryDecodeExodusTransactionAsync(tx.GetHash(), cancellationToken);

            if (exodus != null)
            {
                #pragma warning disable CS0618
                tx.SetExodusTransaction(exodus); // lgtm [cs/call-to-obsolete-method]
                #pragma warning restore CS0618
            }
        }

        async Task<ExodusTransaction> TryDecodeExodusTransactionAsync(uint256 tx, CancellationToken cancellationToken)
        {
            ExodusTransactionInformation info;
            byte[] payload;

            using (var rpc = await Factory.CreateExodusInformationRpcAsync(cancellationToken))
            {
                try
                {
                    info = await rpc.GetTransactionAsync(tx, cancellationToken);
                }
                catch (RPCException ex) when (ex.Message == "Not a Exodus Protocol transaction")
                {
                    return null;
                }

                // Check if transaction still in the mempool. If transaction is not in the block we don't know if it
                // valid or not.
                if (info.Valid == null)
                {
                    throw new ArgumentException("The transaction is not confirmed.", nameof(tx));
                }

                if (!info.Valid.Value)
                {
                    return null;
                }

                payload = await rpc.GetPayloadAsync(tx, cancellationToken);
            }

            try
            {
                return Factory.ExodusEncoder.Decode(info.SendingAddress, info.ReferenceAddress, payload);
            }
            catch (TransactionFieldException ex) when (ex.Field == TransactionFieldException.TypeField)
            {
                return null;
            }
        }
    }
}
