using System;
using NBitcoin.Tests;
using Ztm.Zcoin.NBitcoin;

namespace Ztm.Zcoin.Testing
{
    public static class NodeBuilderFactory
    {
        static readonly NodeDownloadData DownloadData = new NodeDownloadData()
        {
            Version = "0.13.8.5",
            RegtestFolderName = "regtest",
            Linux = new NodeOSDownloadData()
            {
                Archive = "zcoin-{0}-linux64.tar.gz",
                DownloadLink = "https://github.com/zcoinofficial/zcoin/releases/download/v{0}/zcoin-{0}-linux64.tar.gz",
                Executable = "zcoin-0.13.8/bin/zcoind",
                Hash = "52f8a722bb1cc5c53e77edf81f6f1300f3b15d53f9cb4fcee90367f11ad84dd9"
            },
            Windows = new NodeOSDownloadData()
            {
                Archive = "zcoin-{0}-win64.zip",
                DownloadLink = "https://github.com/zcoinofficial/zcoin/releases/download/v{0}/zcoin-{0}-win64.zip",
                Executable = "zcoin-0.13.8/bin/zcoind.exe",
                Hash = "601030d927d9d4d9c5066a8fb9ce990a83b8ce5330e983239e5e4319fef03937"
            }
        };

        /// <summary>
        /// Create a new instance of <see cref="NodeBuilder"/> for the specified test suite.
        /// </summary>
        /// <remarks>
        /// This method is not thread-safe.
        /// </remarks>
        public static NodeBuilder CreateNodeBuilder(Type suite)
        {
            if (suite == null)
            {
                throw new ArgumentNullException(nameof(suite));
            }

            return NodeBuilder.Create(DownloadData, ZcoinNetworks.Instance.Regtest, suite.FullName);
        }
    }
}
