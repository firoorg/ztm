using NBitcoin;

namespace Ztm.Data.Entity.Contexts.Main
{
    public sealed class ExodusPayload
    {
        public uint256 TransactionHash { get; set; }
        public string Sender { get; set; }
        public string Receiver { get; set; }
        public byte[] Data { get; set; }
    }
}