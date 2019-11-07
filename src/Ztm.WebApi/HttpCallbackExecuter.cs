using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Ztm.WebApi
{
    public class HttpCallbackExecuter : ICallbackExecuter
    {
        readonly IHttpClientFactory clientFactory;

        public HttpCallbackExecuter(IHttpClientFactory clientFactory)
        {
            this.clientFactory = clientFactory;
        }

        public async Task<bool> Execute(Guid id, Uri url, CallbackResult result, CancellationToken cancellationToken)
        {
            var request = new HttpRequestMessage
            (
                HttpMethod.Post,
                url
            );

            request.Headers.Add("X-Callback-ID", id.ToString());
            request.Headers.Add("X-Callback-Status", result.Status);

            var content = JsonConvert.SerializeObject(result.Data);
            request.Content = new StringContent(content, Encoding.UTF8, "application/json");

            var client = this.clientFactory.CreateClient();
            var response = await client.SendAsync(request);

            switch(response.StatusCode)
            {
            case HttpStatusCode.OK:
            case HttpStatusCode.Accepted:
                return true;
            }

            return false;
        }
    }
}