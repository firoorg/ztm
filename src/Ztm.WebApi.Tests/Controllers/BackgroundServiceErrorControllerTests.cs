using System;
using System.Collections.Generic;
using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using Ztm.Hosting.AspNetCore;
using Ztm.WebApi.Controllers;
using Ztm.WebApi.Models;

namespace Ztm.WebApi.Tests.Controllers
{
    public sealed class BackgroundServiceErrorControllerTests
    {
        readonly Mock<IBackgroundServiceExceptionHandlerFeature> feature;
        readonly Mock<IFeatureCollection> features;
        readonly Mock<HttpContext> context;
        readonly Mock<IHostingEnvironment> hostingEnvironment;
        readonly BackgroundServiceErrorController subject;

        public BackgroundServiceErrorControllerTests()
        {
            this.feature = new Mock<IBackgroundServiceExceptionHandlerFeature>();

            this.features = new Mock<IFeatureCollection>();

            this.context = new Mock<HttpContext>();
            this.context.Setup(c => c.Features).Returns(this.features.Object);

            this.hostingEnvironment = new Mock<IHostingEnvironment>();

            this.subject = new BackgroundServiceErrorController(this.hostingEnvironment.Object);
            this.subject.ControllerContext.HttpContext = context.Object;
        }

        [Fact]
        public void Constructor_WithNullEnvironment_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "hostingEnvironment",
                () => new BackgroundServiceErrorController(null)
            );
        }

        [Theory]
        [InlineData("Development")]
        [InlineData("Staging")]
        [InlineData("Production")]
        public void BackgroundServiceError_NoErrors_ShouldNotPopulateErrors(string environment)
        {
            // Assert.
            this.hostingEnvironment.Setup(e => e.EnvironmentName).Returns(environment);

            // Act.
            var result = this.subject.BackgroundServiceError().Should().BeOfType<ObjectResult>().Subject;

            // Assert.
            var response = result.Value.Should().BeOfType<BackgroundServiceErrorProblemDetails>().Subject;

            response.Status.Should().Be((int)HttpStatusCode.InternalServerError);
            response.Title.Should().NotBeNull();
            response.Errors.Should().BeNull();

            result.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
        }

        [Theory]
        [InlineData("Staging")]
        [InlineData("Production")]
        public void BackgroundServiceError_HaveErrorsButNotDevelopmentEnvironment_ShouldNotPopulateErrors(string environment)
        {
            // Assert.
            this.features.Setup(c => c.Get<IBackgroundServiceExceptionHandlerFeature>()).Returns(this.feature.Object);

            this.hostingEnvironment.Setup(e => e.EnvironmentName).Returns(environment);

            // Act.
            var result = this.subject.BackgroundServiceError().Should().BeOfType<ObjectResult>().Subject;

            // Assert.
            var response = result.Value.Should().BeOfType<BackgroundServiceErrorProblemDetails>().Subject;

            response.Status.Should().Be((int)HttpStatusCode.InternalServerError);
            response.Title.Should().NotBeNull();
            response.Errors.Should().BeNull();

            result.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
        }

        [Fact]
        public void BackgroundServiceError_HaveErrorsAndInDevelopmentEnvironment_ShouldPopulateErrors()
        {
            // Arrange.
            var message = "Something went wrong.";
            Exception exception;

            try
            {
                // We need to do this so StackTrace will not be null.
                throw new Exception(message);
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            var error = new Hosting.BackgroundServiceError(typeof(string), exception);

            this.feature.Setup(f => f.Errors).Returns(new[] { error });
            this.features.Setup(c => c.Get<IBackgroundServiceExceptionHandlerFeature>()).Returns(this.feature.Object);

            this.hostingEnvironment.Setup(e => e.EnvironmentName).Returns("Development");

            // Act.
            var result = this.subject.BackgroundServiceError().Should().BeOfType<ObjectResult>().Subject;

            // Assert.
            var response = result.Value.Should().BeOfType<BackgroundServiceErrorProblemDetails>().Subject;

            response.Status.Should().Be((int)HttpStatusCode.InternalServerError);
            response.Title.Should().NotBeNull();
            response.Errors.Should().HaveCount(1)
                           .And.BeAssignableTo<IEnumerable<BackgroundServiceError>>()
                           .Which.Should().ContainSingle(
                               e => e.Service == typeof(string).FullName && e.Error == message && e.Detail != null
                           );

            result.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
        }
    }
}
