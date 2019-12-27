using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using NBitcoin;
using NBitcoin.RPC;
using NetMQ;
using NetMQ.Sockets;
using Ztm.Configuration;
using Ztm.Zcoin.Rpc;

namespace Ztm.Zcoin.Synchronization
{
    public sealed class BlocksRetriever : IBlocksRetriever
    {
        readonly ZcoinConfiguration config;
        readonly IRpcFactory rpc;
        SubscriberSocket subscriber;
        NetMQPoller poller;
        SemaphoreSlim newBlockNotification; // lgtm[cs/missed-using-statement]
        CancellationTokenSource retrieveBlocksCancelSource;
        Task retrieveBlocksTask;
        bool disposed;

        public BlocksRetriever(IConfiguration config, IRpcFactory rpc)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (rpc == null)
            {
                throw new ArgumentNullException(nameof(rpc));
            }

            this.config = config.GetZcoinSection();
            this.rpc = rpc;
        }

        public bool IsRunning => this.retrieveBlocksTask != null;

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            if (IsRunning)
            {
                StopAsync(CancellationToken.None).Wait();
            }

            Debug.Assert(this.subscriber == null);
            Debug.Assert(this.poller == null);
            Debug.Assert(this.newBlockNotification == null);
            Debug.Assert(this.retrieveBlocksCancelSource == null);
            Debug.Assert(this.retrieveBlocksTask == null);

            this.disposed = true;
        }

        public Task<Task> StartAsync(IBlocksRetrieverHandler handler, CancellationToken cancellationToken)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            ThrowIfAlreadyDisposed();
            ThrowIfAlreadyRunning();

            Debug.Assert(this.subscriber == null);
            Debug.Assert(this.poller == null);
            Debug.Assert(this.newBlockNotification == null);
            Debug.Assert(this.retrieveBlocksCancelSource == null);
            Debug.Assert(this.retrieveBlocksTask == null);

            // Subscribe to ZeroMQ.
            this.subscriber = new SubscriberSocket();

            try
            {
                this.poller = new NetMQPoller();

                try
                {
                    this.subscriber.ReceiveReady += (sender, e) => NotifyNewBlock();
                    this.subscriber.Connect(this.config.ZeroMq.Address);
                    this.subscriber.Subscribe("hashblock");

                    this.poller.Add(this.subscriber);
                    this.poller.RunAsync();

                    // Start background tasks to retrieve blocks.
                    this.retrieveBlocksCancelSource = new CancellationTokenSource();

                    try
                    {
                        this.retrieveBlocksTask = Task.Run(() => RetrieveBlocks(handler));
                    }
                    catch
                    {
                        this.retrieveBlocksCancelSource.Dispose();
                        this.retrieveBlocksCancelSource = null;
                        throw;
                    }
                }
                catch
                {
                    this.poller.Dispose();
                    this.poller = null;
                    throw;
                }
            }
            catch
            {
                this.subscriber.Dispose();
                this.subscriber = null;
                throw;
            }

            return Task.FromResult(this.retrieveBlocksTask);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            ThrowIfAlreadyDisposed();
            ThrowIfNotRunning();

            // Trigger cancel.
            this.retrieveBlocksCancelSource.Cancel();

            // Wait until background tasks is completed.
            // We need to use Task.WhenAny() so it will not throw if there is exception in retrieveBlocksTask.
            await Task.WhenAny(this.retrieveBlocksTask);

            Debug.Assert(this.newBlockNotification == null);

            // Stop notification listenning.
            this.subscriber.Unsubscribe("hashblock");
            this.subscriber.Disconnect(this.config.ZeroMq.Address);
            this.poller.Stop();

            // Reset state.
            this.retrieveBlocksCancelSource.Dispose();
            this.retrieveBlocksCancelSource = null;
            this.retrieveBlocksTask.Dispose();
            this.retrieveBlocksTask = null;
            this.subscriber.Dispose();
            this.subscriber = null;
            this.poller.Dispose();
            this.poller = null;
        }

        async Task RetrieveBlocks(IBlocksRetrieverHandler handler)
        {
            var cancellationToken = this.retrieveBlocksCancelSource.Token;
            var height = await handler.GetBlockHintAsync(cancellationToken);

            while (true)
            {
                // Get block.
                Block block;

                using (var rpc = await this.rpc.CreateChainInformationRpcAsync(cancellationToken))
                {
                    try
                    {
                        block = await rpc.GetBlockAsync(height, cancellationToken);
                    }
                    catch (RPCException ex) when (ex.RPCCode == RPCErrorCode.RPC_INVALID_PARAMETER)
                    {
                        // Invalid block height.
                        await WaitNewBlockAsync(cancellationToken);
                        continue;
                    }
                }

                // Execute handler.
                height = await handler.ProcessBlockAsync(block, height, cancellationToken);
            }
        }

        void NotifyNewBlock()
        {
            // Read received message to remove it from buffer.
            var topic = this.subscriber.ReceiveFrameString();

            this.subscriber.ReceiveFrameBytes(); // Block hash.
            this.subscriber.ReceiveFrameBytes(); // Sequence.

            Debug.Assert(topic == "hashblock");

            // Raise event.
            var trigger = this.newBlockNotification;
            if (trigger == null)
            {
                return;
            }

            lock (trigger)
            {
                try
                {
                    trigger.Release();
                }
                catch (ObjectDisposedException)
                {
                    // Ignore.
                }
            }
        }

        async Task WaitNewBlockAsync(CancellationToken cancellationToken)
        {
            this.newBlockNotification = new SemaphoreSlim(initialCount: 0);

            try
            {
                await this.newBlockNotification.WaitAsync(cancellationToken);
            }
            finally
            {
                lock (this.newBlockNotification)
                {
                    this.newBlockNotification.Dispose();
                }
                this.newBlockNotification = null;
            }
        }

        void ThrowIfAlreadyDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

        void ThrowIfAlreadyRunning()
        {
            if (IsRunning)
            {
                throw new InvalidOperationException("The Blocks Retriever is being running.");
            }
        }

        void ThrowIfNotRunning()
        {
            if (!IsRunning)
            {
                throw new InvalidOperationException("The Blocks Retriever is not running.");
            }
        }
    }
}
