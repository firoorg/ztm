using NBitcoin;

namespace Ztm.Data.Entity.Contexts.Main
{
    public sealed class ExodusPayload
    {
        public uint256 TransactionHash { get; set; }
        public Script Sender { get; set; }
        public Script Receiver { get; set; }
        public byte[] Data { get; set; }
    }
}