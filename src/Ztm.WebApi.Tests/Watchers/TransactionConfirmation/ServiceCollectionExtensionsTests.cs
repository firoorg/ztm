using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Ztm.WebApi.Watchers.TransactionConfirmation;

namespace Ztm.WebApi.Tests.Watchers.TransactionConfirmation
{
    public sealed class ServiceCollectionExtensionsTests
    {
        readonly ServiceCollection subject;

        public ServiceCollectionExtensionsTests()
        {
            this.subject = new ServiceCollection();
        }

        [Fact]
        public void AddTransactionConfirmationWatcher_WhenInvoke_ShouldAddTransactionConfirmationWatcherServices()
        {
            // Act.
            this.subject.AddTransactionConfirmationWatcher();

            // Assert.
            Assert.Single(this.subject, d => d.ServiceType == typeof(IRuleRepository));
            Assert.Single(this.subject, d => d.ServiceType == typeof(IWatchRepository));
            Assert.Single(this.subject, d => d.ServiceType == typeof(ITransactionConfirmationWatcher));
        }
    }
}
