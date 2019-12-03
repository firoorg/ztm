using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Ztm.Hosting.AspNetCore.Tests
{
    public sealed class ApplicationBuilderExtensionsTests
    {
        readonly Mock<IFeatureCollection> features;
        readonly Mock<HttpRequest> request;
        readonly Collection<BackgroundServiceError> errors;
        readonly Mock<IBackgroundServiceErrorCollector> collector;
        readonly Mock<IServiceProvider> services;
        readonly Mock<HttpContext> context;
        readonly Mock<RequestDelegate> next;
        readonly Mock<IApplicationBuilder> app;
        RequestDelegate middleware;

        public ApplicationBuilderExtensionsTests()
        {
            this.features = new Mock<IFeatureCollection>();

            this.request = new Mock<HttpRequest>();

            this.errors = new Collection<BackgroundServiceError>();

            this.collector = new Mock<IBackgroundServiceErrorCollector>();
            this.collector.Setup(c => c.GetEnumerator())
                          .Returns(() => this.errors.GetEnumerator());

            this.services = new Mock<IServiceProvider>();
            this.services.Setup(s => s.GetService(typeof(IBackgroundServiceErrorCollector)))
                         .Returns(this.collector.Object);

            this.context = new Mock<HttpContext>();
            this.context.Setup(c => c.Features)
                        .Returns(this.features.Object);
            this.context.Setup(c => c.Request)
                        .Returns(this.request.Object);
            this.context.Setup(c => c.RequestServices)
                        .Returns(this.services.Object);

            this.next = new Mock<RequestDelegate>();

            this.app = new Mock<IApplicationBuilder>();
            this.app.Setup(a => a.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>()))
                    .Callback<Func<RequestDelegate, RequestDelegate>>(f => this.middleware = f(this.next.Object));
        }

        [Fact]
        public void AddBackgroundServiceExceptionHandler_WhenInvoke_ShouldRegisterRequiredServices()
        {
            var services = new ServiceCollection();

            services.AddBackgroundServiceExceptionHandler();

            services.Should().HaveCount(3)
                    .And.Contain(d => d.ServiceType == typeof(BackgroundServiceExceptionHandler) && d.Lifetime == ServiceLifetime.Singleton)
                    .And.Contain(d => d.ServiceType == typeof(IBackgroundServiceExceptionHandler) && d.Lifetime == ServiceLifetime.Singleton)
                    .And.Contain(d => d.ServiceType == typeof(IBackgroundServiceErrorCollector) && d.Lifetime == ServiceLifetime.Singleton);
        }

        [Fact]
        public void UseBackgroundServiceExceptionHandler_WithNullPath_ShouldThrow()
        {
            this.app.Object.Invoking(s => s.UseBackgroundServiceExceptionHandler(null))
                           .Should().ThrowExactly<ArgumentNullException>()
                           .And.ParamName.Should().Be("path");
        }

        [Fact]
        public void UseBackgroundServiceExceptionHandler_WhenInvoke_ShouldRegisterMiddleware()
        {
            this.app.Object.UseBackgroundServiceExceptionHandler("/background-service-error");

            this.app.Verify(a => a.Use(It.IsNotNull<Func<RequestDelegate, RequestDelegate>>()));
        }

        [Fact]
        public async Task UseBackgroundServiceExceptionHandler_InvokeMiddlewareWithNoErrors_ShouldNotRedirect()
        {
            // Arrange.
            this.app.Object.UseBackgroundServiceExceptionHandler("/background-service-error");

            // Act.
            await this.middleware(this.context.Object);

            // Assert.
            this.features.Verify(f => f.Set<IBackgroundServiceExceptionHandlerFeature>(It.IsAny<IBackgroundServiceExceptionHandlerFeature>()), Times.Never());
            this.request.VerifySet(r => r.Path = It.IsAny<PathString>(), Times.Never());
            this.next.Verify(f => f(this.context.Object), Times.Once());
        }

        [Fact]
        public async Task UseBackgroundServiceExceptionHandler_InvokeMiddlewareWithErrors_ShouldRedirect()
        {
            // Arrange.
            this.app.Object.UseBackgroundServiceExceptionHandler("/background-service-error");

            this.errors.Add(new BackgroundServiceError(typeof(string), new Exception()));

            // Act.
            await this.middleware(this.context.Object);

            // Assert.
            this.features.Verify(
                f => f.Set<IBackgroundServiceExceptionHandlerFeature>(It.Is<IBackgroundServiceExceptionHandlerFeature>(i => ReferenceEquals(i.Errors, this.collector.Object))),
                Times.Once()
            );
            this.request.VerifySet(
                r => r.Path = "/background-service-error",
                Times.Once()
            );
            this.next.Verify(
                f => f(this.context.Object),
                Times.Once()
            );
        }
    }
}
