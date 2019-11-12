using NBitcoin;

namespace Ztm.Zcoin.NBitcoin.Exodus
{
    public abstract class ExodusTransaction
    {
        public const int MaxId = ushort.MaxValue;
        public const int MaxVersion = ushort.MaxValue;
        public const int MinId = ushort.MinValue;
        public const int MinVersion = ushort.MinValue;

        protected ExodusTransaction(BitcoinAddress sender, BitcoinAddress receiver)
        {
            Sender = sender;
            Receiver = receiver;
        }

        public abstract int Id { get; }

        public BitcoinAddress Receiver { get; }

        public BitcoinAddress Sender { get; }

        public abstract int Version { get; }

        public static bool IsValidId(int id)
        {
            return id >= MinId && id <= MaxId;
        }

        public static bool IsValidVersion(int version)
        {
            return version >= MinVersion && version <= MaxVersion;
        }
    }
}
