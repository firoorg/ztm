using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using NBitcoin;
using Ztm.Zcoin.Synchronization;
using Ztm.Zcoin.Synchronization.Watchers;

namespace Ztm.WebApi
{
    using WatchRepository = ITransactionConfirmationWatchRepository<TransactionConfirmationCallbackResult>;
    using ConfirmContext = TransactionConfirmationWatch<TransactionConfirmationCallbackResult>;

    public sealed class TransactionConfirmationWatcher : IHostedService, IBlockListener
    {
        readonly Ztm.Zcoin.Synchronization.Watchers.TransactionConfirmationWatcher<ConfirmContext> watcher;
        readonly TransactionConfirmationWatcherHandler handler;

        public TransactionConfirmationWatcher(
            ICallbackRepository callbackRepository,
            WatchRepository watchRepository,
            IBlocksStorage blocks,
            ICallbackExecuter callbackExecuter)
        {
            this.handler = new TransactionConfirmationWatcherHandler
            (
                callbackRepository,
                watchRepository,
                callbackExecuter
            );

            this.watcher = new Zcoin.Synchronization.Watchers.TransactionConfirmationWatcher<ConfirmContext>
            (
                handler,
                blocks
            );
        }

        public async Task AddTransactionAsync(
            uint256 transaction,
            int confirmation,
            TimeSpan timeout,
            IPAddress registeringIp,
            Uri callbackUrl,
            TransactionConfirmationCallbackResult successData,
            TransactionConfirmationCallbackResult timeoutData,
            CancellationToken cancellationToken)
        {
            await this.handler.AddTransactionAsync
            (
                transaction,
                confirmation,
                timeout,
                registeringIp,
                callbackUrl,
                successData,
                timeoutData,
                cancellationToken
            );
        }

        public Task BlockAddedAsync(Block block, int height, CancellationToken cancellationToken)
        {
            return this.watcher.ExecuteAsync(block, height, BlockEventType.Added, cancellationToken);
        }

        public Task BlockRemovingAsync(Block block, int height, CancellationToken cancellationToken)
        {
            return this.watcher.ExecuteAsync(block, height, BlockEventType.Removing, cancellationToken);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return this.handler.Initialize(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return this.handler.StopAllTimers(cancellationToken);
        }
    }
}