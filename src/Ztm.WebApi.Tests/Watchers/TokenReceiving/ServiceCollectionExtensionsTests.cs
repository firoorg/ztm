using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Ztm.WebApi.Watchers.TokenReceiving;

namespace Ztm.WebApi.Tests.Watchers.TokenReceiving
{
    public sealed class ServiceCollectionExtensionsTests
    {
        readonly ServiceCollection subject;

        public ServiceCollectionExtensionsTests()
        {
            this.subject = new ServiceCollection();
        }

        [Fact]
        public void AddTokenReceivingWatcher_WhenInvoke_ShouldAddTokenReceivingWatcherServices()
        {
            // Act.
            this.subject.AddTokenReceivingWatcher();

            // Assert.
            Assert.Single(this.subject, d => d.ServiceType == typeof(IRuleRepository));
            Assert.Single(this.subject, d => d.ServiceType == typeof(IWatchRepository));
            Assert.Single(this.subject, d => d.ServiceType == typeof(ITokenReceivingWatcher));
        }
    }
}
