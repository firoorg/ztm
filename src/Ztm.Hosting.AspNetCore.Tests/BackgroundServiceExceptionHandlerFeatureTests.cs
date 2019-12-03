using System;
using System.Collections.ObjectModel;
using FluentAssertions;
using Xunit;

namespace Ztm.Hosting.AspNetCore.Tests
{
    public sealed class BackgroundServiceExceptionHandlerFeatureTests
    {
        readonly Collection<BackgroundServiceError> errors;
        readonly BackgroundServiceExceptionHandlerFeature subject;

        public BackgroundServiceExceptionHandlerFeatureTests()
        {
            this.errors = new Collection<BackgroundServiceError>();
            this.subject = new BackgroundServiceExceptionHandlerFeature(this.errors);
        }

        [Fact]
        public void Constructor_WithNullErrors_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>("errors", () => new BackgroundServiceExceptionHandlerFeature(null));
        }

        [Fact]
        public void Constructor_WhenSuccess_ShouldInitializeProperties()
        {
            this.subject.Errors.Should().BeSameAs(this.errors);
        }
    }
}
