using System.IO;
using NBitcoin;

namespace Ztm.Zcoin.NBitcoin.Exodus
{
    public interface ITransactionPayloadEncoder
    {
        int Type { get; }

        void Encode(BinaryWriter writer, ExodusTransaction transaction);
        ExodusTransaction Decode(BitcoinAddress sender, BitcoinAddress receiver, BinaryReader payload, int version);
    }
}
