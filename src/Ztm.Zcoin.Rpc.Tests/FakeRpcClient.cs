using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NBitcoin;
using NBitcoin.RPC;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.Zcoin.Rpc.Tests
{
    sealed class FakeRpcClient : RpcClient
    {
        public FakeRpcClient(RpcFactory factory, RPCClient client) : base(factory, client)
        {
            StubbedDispose = new Mock<Action<bool>>();
        }

        public new RPCClient Client => base.Client;

        public new RpcFactory Factory => base.Factory;

        public Mock<Action<bool>> StubbedDispose { get; }

        public static new byte ToNative(Ecosystem ecosystem)
        {
            return RpcClient.ToNative(ecosystem);
        }

        public static new uint ToNative(PropertyId id)
        {
            return RpcClient.ToNative(id);
        }

        public static new ushort ToNative(PropertyType type)
        {
            return RpcClient.ToNative(type);
        }

        public new Task PopulateExodusInformationAsync(Transaction tx, CancellationToken cancellationToken)
        {
            return base.PopulateExodusInformationAsync(tx, cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            StubbedDispose.Object(disposing);
        }
    }
}
