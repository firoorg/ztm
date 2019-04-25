using System.Runtime.CompilerServices;
using NBitcoin.Tests;
using Ztm.Zcoin.NBitcoin;

namespace Ztm.Zcoin.Testing
{
    public static class NodeBuilderFactory
    {
        public static NodeBuilder CreateNodeBuilder([CallerMemberName] string directoryName = null)
        {
            var data = new NodeDownloadData()
            {
                Version = "0.13.7.7",
                RegtestFolderName = "regtest",
                Linux = new NodeOSDownloadData()
                {
                    Archive = "zcoin-{0}-linux64.tar.gz",
                    DownloadLink = "https://github.com/zcoinofficial/zcoin/releases/download/v{0}/zcoin-{0}-linux64.tar.gz",
                    Executable = "zcoin-0.13.7/bin/zcoind",
                    Hash = "efcb949f55ec4f1b2764aacccb12420ad22d9b759d4b4554d682563bcb4792d4"
                },
                Windows = new NodeOSDownloadData()
                {
                    Archive = "zcoin-{0}-win64.zip",
                    DownloadLink = "https://github.com/zcoinofficial/zcoin/releases/download/v{0}/zcoin-{0}-win64.zip",
                    Executable = "zcoin-0.13.7/bin/zcoind.exe",
                    Hash = "6b047f8485f906d32d7ccbfd84371510d9648792d9824ccf0598acdb0bcdfb7d"
                }
            };

            return NodeBuilder.Create(data, ZcoinNetworks.Instance.Regtest, directoryName);
        }
    }
}
