using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NSubstitute;
using Xunit;
using Ztm.WebApi.Callbacks;

namespace Ztm.WebApi.Tests.Callbacks
{
    public sealed class HttpCallbackExecuterTests
    {
        readonly IHttpClientFactory factory;
        readonly HttpClient client;
        readonly HttpCallbackExecuter subject;

        public HttpCallbackExecuterTests()
        {
            this.client = Substitute.ForPartsOf<HttpClient>();
            this.factory = Substitute.For<IHttpClientFactory>();

            this.factory.CreateClient().Returns(this.client);

            this.subject = new HttpCallbackExecuter(this.factory);
        }

        [Fact]
        public void Construct_WithNullFactory_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "factory",
                () => new HttpCallbackExecuter(null)
            );
        }

        [Fact]
        public async Task ExecuteAsync_WithNullArguments_ShouldThrow()
        {
            var result = new CallbackResult("", "");
            var url = new Uri("https://zcoin.io/callback");

            await Assert.ThrowsAsync<ArgumentNullException>(
                "url",
                () => this.subject.ExecuteAsync(Guid.Empty, null, result, CancellationToken.None)
            );

            await Assert.ThrowsAsync<ArgumentNullException>(
                "result",
                () => this.subject.ExecuteAsync(Guid.Empty, url, null, CancellationToken.None)
            );
        }

        [Theory]
        [InlineData(HttpStatusCode.Accepted)]
        [InlineData(HttpStatusCode.OK)]
        public async Task ExecuteAsync_WithValidArgumentsAndNetworkSuccess_ShouldSuccess(HttpStatusCode responseStatus)
        {
            // Arrange.
            var payload = "Callback data";

            var result = new CallbackResult("", payload);
            var id = Guid.NewGuid();
            var url = new Uri("https://zcoin.io/callback");
            var cancellationToken = new CancellationToken(false);

            var response = new HttpResponseMessage();
            response.StatusCode = responseStatus;

            HttpRequestMessage request = null;
            string requestContent = null;

            this.client
                .SendAsync(Arg.Do<HttpRequestMessage>(
                    async m => {
                        // Intercept for assertion.
                        requestContent = await m.Content.ReadAsStringAsync();
                        request = m;
                    }), Arg.Is<CancellationToken>(c => c == cancellationToken))
                .Returns(Task.FromResult(response));

            // Act.
            await this.subject.ExecuteAsync(id, url, result, cancellationToken);

            // Assert.
            _ = this.client.Received(1).SendAsync
            (
                Arg.Any<HttpRequestMessage>(),
                Arg.Is<CancellationToken>(c => c == cancellationToken)
            );

            Assert.Equal(url, request.RequestUri);

            Assert.True(request.Headers.TryGetValues("X-Callback-ID", out var callbackIds));
            Assert.Single(callbackIds);
            Assert.Equal(id.ToString(), callbackIds.First());

            Assert.True(request.Headers.TryGetValues("X-Callback-Status", out var callbackStatuses));
            Assert.Single(callbackStatuses);
            Assert.Equal(result.Status, callbackStatuses.First());

            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("application/json; charset=utf-8", request.Content.Headers.ContentType.ToString());

            var deserialized = JsonConvert.DeserializeObject<string>(requestContent);
            Assert.Equal(payload, deserialized);
        }

        [Theory]
        [InlineData(HttpStatusCode.Accepted)]
        [InlineData(HttpStatusCode.OK)]
        public async Task ExecuteAsync_WithValidArgumentsAndNetworkSuccess_WithNullData_PayloadShouldNotBeSet(HttpStatusCode responseStatus)
        {
            // Arrange.
            var result = new CallbackResult("", null);
            var id = Guid.NewGuid();
            var url = new Uri("https://zcoin.io/callback");
            var cancellationToken = new CancellationToken(false);

            var response = new HttpResponseMessage();
            response.StatusCode = responseStatus;

            HttpRequestMessage request = null;

            this.client
                .SendAsync(Arg.Is<HttpRequestMessage>(m => m.Content == null), Arg.Is<CancellationToken>(c => c == cancellationToken))
                .Returns(r => {
                    // Intercept for assertion.
                    request = r.ArgAt<HttpRequestMessage>(0);
                    return Task.FromResult(response);
                });

            // Act.
            await this.subject.ExecuteAsync(id, url, result, cancellationToken);

            // Assert.
            _ = this.client.Received(1).SendAsync
            (
                Arg.Any<HttpRequestMessage>(),
                Arg.Is<CancellationToken>(c => c == cancellationToken)
            );

            Assert.Equal(url, request.RequestUri);

            Assert.True(request.Headers.TryGetValues("X-Callback-ID", out var callbackIds));
            Assert.Single(callbackIds);
            Assert.Equal(id.ToString(), callbackIds.First());

            Assert.True(request.Headers.TryGetValues("X-Callback-Status", out var callbackStatuses));
            Assert.Single(callbackStatuses);
            Assert.Equal(result.Status, callbackStatuses.First());

            Assert.Equal(HttpMethod.Post, request.Method);
        }

        [Fact]
        public async Task ExecuteAsync_WithValidArgumentsAndNetworkFail_ShouldThrow()
        {
            // Arrange.
            var result = new CallbackResult("", null);
            var id = Guid.NewGuid();
            var url = new Uri("https://zcoin.io/callback");
            var cancellationToken = new CancellationToken(false);

            var response = new HttpResponseMessage();
            response.StatusCode = HttpStatusCode.BadRequest;

            this.client
                .SendAsync(Arg.Any<HttpRequestMessage>(), Arg.Is<CancellationToken>(c => c == cancellationToken))
                .Returns(Task.FromResult(response));

            // Act && Assert.
            await Assert.ThrowsAsync<HttpRequestException>(
                () => this.subject.ExecuteAsync(id, url, result, cancellationToken)
            );
        }
    }
}