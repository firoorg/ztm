using System;
using System.Collections.Generic;
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
        readonly IEnumerable<IBlockListener> listeners;
        readonly Network activeNetwork;
        readonly ServiceManager services;
        bool disposed;

        public BlocksSynchronizer(
            IConfiguration config,
            ILogger<BlocksSynchronizer> logger,
            IBlocksRetriever retriever,
            IBlocksStorage storage,
            IEnumerable<IBlockListener> listeners)
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

            if (listeners == null)
            {
                throw new ArgumentNullException(nameof(listeners));
            }

            this.logger = logger;
            this.retriever = retriever;
            this.storage = storage;
            this.listeners = listeners;
            this.activeNetwork = ZcoinNetworks.Instance.GetNetwork(config.GetZcoinSection().Network.Type);
            this.services = new ServiceManager(listeners);

            try
            {
                this.services.Stopped += (sender, e) => ScheduleStop(((ServiceManager)sender).Exception);
            }
            catch
            {
                this.services.Dispose();
                throw;
            }
        }

        public event EventHandler<BlockEventArgs> BlockAdded;

        public event EventHandler<BlockEventArgs> BlockRemoving;

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!this.disposed)
            {
                if (disposing)
                {
                    this.retriever.Dispose();
                    this.services.Dispose();
                }

                this.disposed = true;
            }
        }

        protected override async Task OnStartAsync(CancellationToken cancellationToken)
        {
            await this.services.StartAsync(cancellationToken);
            await this.retriever.StartAsync(this, CancellationToken.None);
        }

        protected override async Task OnStopAsync(CancellationToken cancellationToken)
        {
            await this.retriever.StopAsync(cancellationToken);
            await this.services.StopAsync(cancellationToken);
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

                    foreach (var listener in this.listeners)
                    {
                        await listener.BlockRemovingAsync(localBlock, localHeight);
                    }

                    await BlockRemoving.InvokeAsync(
                        this,
                        new BlockEventArgs(localBlock, localHeight, CancellationToken.None)
                    );

                    await this.storage.RemoveLastAsync(cancellationToken);

                    return localHeight;
                }
            }

            // Store block.
            this.logger.LogInformation("Adding block {Height}:{Hash}", height, block.GetHash());

            await this.storage.AddAsync(block, height, cancellationToken);

            // Raise event.
            foreach (var listener in this.listeners)
            {
                await listener.BlockAddedAsync(block, height);
            }

            await BlockAdded.InvokeAsync(
                this,
                new BlockEventArgs(block, height, CancellationToken.None)
            );

            return height + 1;
        }

        Task IBlocksRetrieverHandler.StopAsync(Exception ex, CancellationToken cancellationToken)
        {
            ScheduleStop(ex);

            return Task.CompletedTask;
        }
    }
}
