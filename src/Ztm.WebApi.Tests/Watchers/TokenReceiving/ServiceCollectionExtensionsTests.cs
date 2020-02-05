using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Ztm.Data.Entity.Contexts;
using Ztm.Threading;
using Ztm.WebApi.AddressPools;
using Ztm.WebApi.Callbacks;
using Ztm.WebApi.Watchers.TokenReceiving;
using Ztm.Zcoin.NBitcoin.Exodus;
using Ztm.Zcoin.NBitcoin.Exodus.TransactionRetrievers;
using Ztm.Zcoin.Synchronization;

namespace Ztm.WebApi.Tests.Watchers.TokenReceiving
{
    public sealed class ServiceCollectionExtensionsTests
    {
        readonly Mock<IReceivingAddressRepository> addresses;
        readonly Mock<IBlocksStorage> blocks;
        readonly Mock<IMainDatabaseFactory> db;
        readonly Mock<ICallbackRepository> callbacks;
        readonly Mock<ICallbackExecuter> executer;
        readonly Mock<ITransactionRetriever> exodusRetriever;
        readonly Mock<ILogger<TokenReceivingWatcher>> logger;
        readonly Mock<IReceivingAddressPool> pool;
        readonly PropertyId property;
        readonly Mock<ITimerScheduler> scheduler;
        readonly ServiceCollection subject;

        public ServiceCollectionExtensionsTests()
        {
            this.addresses = new Mock<IReceivingAddressRepository>();
            this.blocks = new Mock<IBlocksStorage>();
            this.db = new Mock<IMainDatabaseFactory>();
            this.callbacks = new Mock<ICallbackRepository>();
            this.executer = new Mock<ICallbackExecuter>();
            this.exodusRetriever = new Mock<ITransactionRetriever>();
            this.logger = new Mock<ILogger<TokenReceivingWatcher>>();
            this.pool = new Mock<IReceivingAddressPool>();
            this.property = new PropertyId(3);
            this.scheduler = new Mock<ITimerScheduler>();
            this.subject = new ServiceCollection();

            this.subject.AddSingleton(this.addresses.Object);
            this.subject.AddSingleton(this.blocks.Object);
            this.subject.AddSingleton(this.db.Object);
            this.subject.AddSingleton(this.callbacks.Object);
            this.subject.AddSingleton(this.executer.Object);
            this.subject.AddSingleton(this.exodusRetriever.Object);
            this.subject.AddSingleton(this.logger.Object);
            this.subject.AddSingleton(this.pool.Object);
            this.subject.AddSingleton(this.property);
            this.subject.AddSingleton(this.scheduler.Object);
        }

        [Fact]
        public void AddTokenReceivingWatcher_WhenInvoke_ShouldAddTokenReceivingWatcherServices()
        {
            // Act.
            this.subject.AddTokenReceivingWatcher();

            // Assert.
            using (var provider = this.subject.BuildServiceProvider())
            {
                provider.GetRequiredService<IRuleRepository>();
                provider.GetRequiredService<IWatchRepository>();

                var listener = provider.GetRequiredService<IBlockListener>();
                var service = provider.GetRequiredService<IHostedService>();
                var watcher = provider.GetRequiredService<ITokenReceivingWatcher>();

                Assert.Same(listener, service);
                Assert.Same(listener, watcher);
            }
        }
    }
}
