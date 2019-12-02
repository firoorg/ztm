using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Ztm.Hosting.Tests
{
    public sealed class BackgroundServiceErrorCollectorTests
    {
        readonly BackgroundServiceErrorCollector subject;

        public BackgroundServiceErrorCollectorTests()
        {
            this.subject = new BackgroundServiceErrorCollector();
        }

        [Fact]
        public async Task RunAsync_WithValidArguments_ShouldSuccess()
        {
            var ex = new Exception();

            await RunAsync(typeof(FakeBackgroundService), ex, CancellationToken.None);

            this.subject.Should().ContainSingle(e => e.Service == typeof(FakeBackgroundService) && e.Exception == ex);
        }

        [Theory]
        [InlineData(0, true)]
        [InlineData(0, false)]
        [InlineData(1, true)]
        [InlineData(1, false)]
        [InlineData(2, true)]
        [InlineData(2, false)]
        public async Task GetEnumerator_WhenIterate_ShouldFoundTheSameAddedItems(int items, bool generic)
        {
            // Arrange.
            var generated = new Collection<BackgroundServiceError>();

            for (int i = 0; i < items; i++)
            {
                var error = new BackgroundServiceError(typeof(FakeBackgroundService), new Exception());

                await RunAsync(typeof(FakeBackgroundService), new Exception(), CancellationToken.None);
                generated.Add(error);
            }

            // Act.
            var enumerator = generic ? this.subject.GetEnumerator() : ((IEnumerable)this.subject).GetEnumerator();
            var collected = new Collection<BackgroundServiceError>();

            while (enumerator.MoveNext())
            {
                collected.Add((BackgroundServiceError)enumerator.Current);
            }

            // Assert.
            collected.Should().BeEquivalentTo(generated);
        }

        Task RunAsync(Type service, Exception exception, CancellationToken cancellationToken)
        {
            return ((IBackgroundServiceExceptionHandler)this.subject).RunAsync(service, exception, cancellationToken);
        }
    }
}
