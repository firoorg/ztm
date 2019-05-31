using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Ztm.Configuration;
using Ztm.ObjectModel;
using Ztm.ServiceModel;
using Ztm.Zcoin.NBitcoin;

namespace Ztm.Zcoin.Synchronization
{
    public class BlocksSynchronizer : BackgroundService, IBlocksRetrieverHandler, IBlocksSynchronizer
    {
        readonly ILogger logger;
        readonly IBlocksRetriever retriever;
        readonly IBlocksStorage storage;
        readonly Network activeNetwork;
        bool disposed;

        public BlocksSynchronizer(
            IConfiguration config,
            ILogger<BlocksSynchronizer> logger,
            IBlocksRetriever retriever,
            IBlocksStorage storage)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (retriever == null)
            {
                throw new ArgumentNullException(nameof(retriever));
            }

            if (storage == null)
            {
                throw new ArgumentNullException(nameof(storage));
            }

            this.logger = logger;
            this.retriever = retriever;
            this.storage = storage;
            this.activeNetwork = ZcoinNetworks.Instance.GetNetwork(config.GetZcoinSection().Network.Type);
        }

        public override string Name => "Blocks Synchronizer";

        public event EventHandler<BlockEventArgs> BlockAdded;

        public event EventHandler<BlockEventArgs> BlockRemoved;

        protected override void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.retriever.Dispose();
                }

                this.disposed = true;
            }

            base.Dispose(disposing);
        }

        protected override Task OnStartAsync(CancellationToken cancellationToken)
        {
            return this.retriever.StartAsync(this, cancellationToken);
        }

        protected override Task OnStopAsync(CancellationToken cancellationToken)
        {
            return this.retriever.StopAsync(cancellationToken);
        }

        async Task<int> IBlocksRetrieverHandler.GetBlockHintAsync(CancellationToken cancellationToken)
        {
            var (last, height) = await this.storage.GetLastAsync(cancellationToken);

            if (last == null)
            {
                return 0;
            }

            return height + 1;
        }

        async Task<int> IBlocksRetrieverHandler.ProcessBlockAsync(ZcoinBlock block, int height, CancellationToken cancellationToken)
        {
            var (localBlock, localHeight) = await this.storage.GetLastAsync(cancellationToken);

            block.Header.PrecomputeHash(invalidateExisting: false, lazily: false);

            // Make sure passed block is expected one.
            if (localBlock == null)
            {
                if (height != 0)
                {
                    return 0;
                }

                if (block.GetHash() != this.activeNetwork.GetGenesis().GetHash())
                {
                    throw new ArgumentException("Block is not genesis block.", nameof(block));
                }
            }
            else
            {
                if (height != (localHeight + 1))
                {
                    return localHeight + 1;
                }

                localBlock.Header.PrecomputeHash(invalidateExisting: false, lazily: false);

                if (block.Header.HashPrevBlock != localBlock.GetHash())
                {
                    // Our latest block is not what expected (e.g. chain already switched)
                    // so we need to reload it.
                    this.logger.LogInformation(
                        "Block {DaemonHeight}:{DaemonHash} from daemon is not on our chain, discarding our last block ({LocalHeight}:{LocalHash})",
                        height,
                        block.GetHash(),
                        localHeight,
                        localBlock.GetHash()
                    );

                    await this.storage.RemoveLastAsync(cancellationToken);
                    await BlockRemoved.InvokeAsync(
                        this,
                        new BlockEventArgs(localBlock, localHeight, cancellationToken)
                    );

                    return localHeight;
                }
            }

            // Store block.
            this.logger.LogInformation("Adding block {Height:Hash}", height, block.GetHash());

            await this.storage.AddAsync(block, height, cancellationToken);
            await BlockAdded.InvokeAsync(
                this,
                new BlockEventArgs(block, height, cancellationToken)
            );

            return height + 1;
        }

        Task IBlocksRetrieverHandler.StopAsync(Exception ex, CancellationToken cancellationToken)
        {
            Exception = ex;

            BeginStop();

            return Task.CompletedTask;
        }
    }
}
