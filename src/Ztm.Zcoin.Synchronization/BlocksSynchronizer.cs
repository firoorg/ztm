using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using NBitcoin;
using Ztm.Configuration;
using Ztm.ObjectModel;
using Ztm.Zcoin.NBitcoin;

namespace Ztm.Zcoin.Synchronization
{
    public sealed class BlocksSynchronizer : IBlocksRetrieverHandler, IBlocksSynchronizer, IDisposable
    {
        readonly IBlocksRetriever retriever;
        readonly IBlocksStorage storage;
        readonly Network activeNetwork;
        bool disposed;

        public BlocksSynchronizer(IConfiguration config, IBlocksRetriever retriever, IBlocksStorage storage)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (retriever == null)
            {
                throw new ArgumentNullException(nameof(retriever));
            }

            if (storage == null)
            {
                throw new ArgumentNullException(nameof(storage));
            }

            this.retriever = retriever;
            this.storage = storage;
            this.activeNetwork = ZcoinNetworks.Instance.GetNetwork(config.GetZcoinSection().Network.Type);
        }

        public string Name => "Blocks Synchronizer";

        public event EventHandler<BlockEventArgs> BlockAdded;

        public event EventHandler<BlockEventArgs> BlockRemoved;

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.retriever.Dispose();

            this.disposed = true;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return this.retriever.StartAsync(this, cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
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

                if (block.Header.HashPrevBlock != localBlock.GetHash())
                {
                    // Our latest block is not what expected (e.g. chain already switched)
                    // so we need to reload it.
                    await this.storage.RemoveLastAsync(cancellationToken);
                    await BlockRemoved.InvokeAsync(
                        this,
                        new BlockEventArgs(localBlock, localHeight, cancellationToken)
                    );
                    return localHeight;
                }
            }

            // Store block.
            await this.storage.AddAsync(block, height, cancellationToken);
            await BlockAdded.InvokeAsync(
                this,
                new BlockEventArgs(block, height, cancellationToken)
            );

            return height + 1;
        }

        Task IBlocksRetrieverHandler.StopAsync(Exception ex, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
