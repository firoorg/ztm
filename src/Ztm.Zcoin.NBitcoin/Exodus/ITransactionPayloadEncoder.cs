using System.IO;
using NBitcoin;

namespace Ztm.Zcoin.NBitcoin.Exodus
{
    public interface ITransactionPayloadEncoder
    {
        int Type { get; }

        ExodusTransaction Decode(BitcoinAddress sender, BitcoinAddress receiver, BinaryReader payload, int version);
    }
}
