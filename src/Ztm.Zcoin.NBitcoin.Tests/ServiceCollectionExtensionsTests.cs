using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Ztm.Zcoin.NBitcoin.Exodus;
using Ztm.Zcoin.NBitcoin.Exodus.TransactionRetrievers;

namespace Ztm.Zcoin.NBitcoin.Tests
{
    public sealed class ServiceCollectionExtensionsTests
    {
        readonly ServiceCollection subject;

        public ServiceCollectionExtensionsTests()
        {
            this.subject = new ServiceCollection();
        }

        [Fact]
        public void AddNBitcoin_WhenInvoke_ShouldAddAllNBitcoinServices()
        {
            // Act.
            this.subject.AddNBitcoin();

            // Assert.
            Assert.Single(this.subject, IsPayloadEncoder(typeof(SimpleSendEncoder)));
            Assert.Single(this.subject, d => d.ServiceType == typeof(ITransactionEncoder));
            Assert.Single(this.subject, IsTransactionRetriever(typeof(SimpleSendRetriever)));
            Assert.Single(this.subject, d => d.ServiceType == typeof(ITransactionRetriever));
        }

        static Predicate<ServiceDescriptor> IsPayloadEncoder(Type type)
        {
            return d => d.ServiceType == typeof(ITransactionPayloadEncoder) && d.ImplementationType == type;
        }

        static Predicate<ServiceDescriptor> IsTransactionRetriever(Type type)
        {
            return d => d.ServiceType == typeof(IExodusTransactionRetriever) && d.ImplementationType == type;
        }
    }
}
