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
                Version = "0.13.7.10",
                RegtestFolderName = "regtest",
                Linux = new NodeOSDownloadData()
                {
                    Archive = "zcoin-{0}-linux64.tar.gz",
                    DownloadLink = "https://github.com/zcoinofficial/zcoin/releases/download/v{0}/zcoin-{0}-linux64.tar.gz",
                    Executable = "zcoin-0.13.7/bin/zcoind",
                    Hash = "04b11d4bed070c0131e3b546e5ebdddf174121dd6f39cc02f7f241bc56fb3a8c"
                },
                Windows = new NodeOSDownloadData()
                {
                    Archive = "zcoin-{0}-win64.zip",
                    DownloadLink = "https://github.com/zcoinofficial/zcoin/releases/download/v{0}/zcoin-{0}-win64.zip",
                    Executable = "zcoin-0.13.7/bin/zcoind.exe",
                    Hash = "3f9b49b7eb800deeaa26158dc1f11c7488d6c03e8c20c35f9dac788cc78ce13b"
                }
            };

            return NodeBuilder.Create(data, ZcoinNetworks.Instance.Regtest, directoryName);
        }
    }
}
