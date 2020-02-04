using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Xunit;
using Ztm.Data.Entity.Contexts;
using Ztm.WebApi.Callbacks;
using Ztm.WebApi.Watchers.TransactionConfirmation;
using Ztm.Zcoin.Synchronization;

namespace Ztm.WebApi.Tests.Watchers.TransactionConfirmation
{
    public sealed class ServiceCollectionExtensionsTests
    {
        readonly Mock<IBlocksStorage> blocks;
        readonly Mock<ICallbackRepository> callbacks;
        readonly Mock<IMainDatabaseFactory> db;
        readonly Mock<ICallbackExecuter> executer;
        readonly Mock<ILogger<TransactionConfirmationWatcher>> logger;
        readonly JsonSerializer serializer;
        readonly ServiceCollection subject;

        public ServiceCollectionExtensionsTests()
        {
            this.blocks = new Mock<IBlocksStorage>();
            this.callbacks = new Mock<ICallbackRepository>();
            this.db = new Mock<IMainDatabaseFactory>();
            this.executer = new Mock<ICallbackExecuter>();
            this.logger = new Mock<ILogger<TransactionConfirmationWatcher>>();
            this.serializer = new JsonSerializer();
            this.subject = new ServiceCollection();

            this.subject.AddSingleton(this.blocks.Object);
            this.subject.AddSingleton(this.callbacks.Object);
            this.subject.AddSingleton(this.db.Object);
            this.subject.AddSingleton(this.executer.Object);
            this.subject.AddSingleton(this.logger.Object);
            this.subject.AddSingleton(this.serializer);
        }

        [Fact]
        public void AddTransactionConfirmationWatcher_WhenInvoke_ShouldAddTransactionConfirmationWatcherServices()
        {
            // Act.
            this.subject.AddTransactionConfirmationWatcher();

            // Assert.
            using (var provider = this.subject.BuildServiceProvider())
            {
                provider.GetRequiredService<IRuleRepository>();
                provider.GetRequiredService<IWatchRepository>();

                var listener = provider.GetRequiredService<IBlockListener>();
                var service = provider.GetRequiredService<IHostedService>();
                var watcher = provider.GetRequiredService<ITransactionConfirmationWatcher>();

                Assert.Same(listener, service);
                Assert.Same(listener, watcher);
            }
        }
    }
}
