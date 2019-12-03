using System;
using FluentAssertions;
using Xunit;

namespace Ztm.Hosting.Tests
{
    public sealed class BackgroundServiceErrorTests
    {
        readonly Type service;
        readonly Exception exception;
        readonly BackgroundServiceError subject;

        public BackgroundServiceErrorTests()
        {
            this.service = typeof(FakeBackgroundService);
            this.exception = new Exception();
            this.subject = new BackgroundServiceError(this.service, this.exception);
        }

        [Fact]
        public void Constructor_WithNullService_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>("service", () => new BackgroundServiceError(null, this.exception));
        }

        [Fact]
        public void Constructor_WithNullException_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>("exception", () => new BackgroundServiceError(this.service, null));
        }

        [Fact]
        public void Constructor_WithValidParameters_ShouldSuccess()
        {
            this.subject.Exception.Should().BeSameAs(this.exception);
            this.subject.Service.Should().BeSameAs(this.service);
        }
    }
}
