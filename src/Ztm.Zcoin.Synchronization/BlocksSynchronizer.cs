using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Ztm.Hosting;

namespace Ztm.Zcoin.Synchronization
{
    public sealed class BlocksSynchronizer : BackgroundService, IBlocksRetrieverHandler
    {
        readonly ILogger logger;
        readonly IBlocksRetriever retriever;
        readonly IBlocksStorage storage;
        readonly IEnumerable<IBlockListener> listeners;
        readonly Network chainNetwork;

        public BlocksSynchronizer(
            IBackgroundServiceExceptionHandler exceptionHandler,
            Network network,
            ILogger<BlocksSynchronizer> logger,
            IBlocksRetriever retriever,
            IBlocksStorage storage,
            IEnumerable<IBlockListener> listeners) : base(exceptionHandler)
        {
            if (network == null)
            {
                throw new ArgumentNullException(nameof(network));
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
            this.chainNetwork = network;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var retrieverTask = await this.retriever.StartAsync(this, cancellationToken);

            try
            {
                var stop = new TaskCompletionSource<bool>();
                cancellationToken.Register(() => stop.SetResult(true));

                var stopped = await Task.WhenAny(retrieverTask, stop.Task);
                await stopped;
            }
            finally
            {
                await this.retriever.StopAsync(CancellationToken.None);
            }
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

        async Task<int> IBlocksRetrieverHandler.ProcessBlockAsync(Block block, int height, CancellationToken cancellationToken)
        {
            var (localBlock, localHeight) = await this.storage.GetLastAsync(cancellationToken);

            // Make sure passed block is expected one.
            if (localBlock == null)
            {
                if (height != 0)
                {
                    return 0;
                }

                if (block.GetHash() != this.chainNetwork.GetGenesis().GetHash())
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
                        // Don't allow to cancel here due to we don't want the first listener sucess but the next one
                        // get cancelled.
                        await listener.BlockRemovingAsync(localBlock, localHeight, CancellationToken.None);
                    }

                    await this.storage.RemoveLastAsync(CancellationToken.None);

                    return localHeight;
                }
            }

            // Store block.
            this.logger.LogInformation("Adding block {Height}:{Hash}", height, block.GetHash());

            await this.storage.AddAsync(block, height, cancellationToken);

            // Raise event.
            foreach (var listener in this.listeners)
            {
                // Don't allow to cancel here due to we don't want the first listener sucess but the next one get
                // cancelled.
                await listener.BlockAddedAsync(block, height, CancellationToken.None);
            }

            return height + 1;
        }
    }
}
