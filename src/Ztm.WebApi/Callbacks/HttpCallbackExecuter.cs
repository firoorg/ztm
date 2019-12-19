using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Ztm.WebApi.Callbacks
{
    public class HttpCallbackExecuter : ICallbackExecuter
    {
        readonly IHttpClientFactory factory;

        public HttpCallbackExecuter(IHttpClientFactory factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            this.factory = factory;
        }

        public async Task ExecuteAsync(Guid id, Uri url, CallbackResult result, CancellationToken cancellationToken)
        {
            if (url == null)
            {
                throw new ArgumentNullException(nameof(url));
            }

            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            using (var client = this.factory.CreateClient())
            using (var request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                request.Headers.Add("X-Callback-ID", id.ToString());
                request.Headers.Add("X-Callback-Status", result.Status);

                if (result.Data != null)
                {
                    var content = JsonConvert.SerializeObject(result.Data);
                    request.Content = new StringContent(content, Encoding.UTF8, "application/json");
                }

                var response = await client.SendAsync(request, cancellationToken);

                switch (response.StatusCode)
                {
                case HttpStatusCode.OK:
                case HttpStatusCode.Accepted:
                    return;
                default:
                    throw new HttpRequestException($"Callback Execution return unexpected status ({response.StatusCode}).");
                }
            }
        }
    }
}