using System;
using System.Net;
using System.Net.Http;

namespace Ztm.EndToEndTests
{
    public sealed class ZtmFixture : IDisposable
    {
        readonly string host;
        readonly int port;
        readonly HttpClient client;

        public ZtmFixture()
        {
            this.host = Environment.GetEnvironmentVariable("ZTM_HOST");

            if (string.IsNullOrEmpty(this.host))
            {
                throw new InvalidOperationException("No ZTM_HOST environment variable is set.");
            }

            try
            {
                this.port = int.Parse(Environment.GetEnvironmentVariable("ZTM_PORT"));
            }
            catch (ArgumentNullException ex)
            {
                throw new InvalidOperationException("No ZTM_PORT environment variable is set.", ex);
            }
            catch (Exception ex) when (ex is FormatException || ex is OverflowException)
            {
                throw new InvalidOperationException("ZTM_PORT environment variable have invalid value.", ex);
            }

            if (this.port < IPEndPoint.MinPort || this.port > IPEndPoint.MaxPort)
            {
                throw new InvalidOperationException("ZTM_PORT environment variable have invalid value.");
            }

            this.client = new HttpClient();
        }

        public void Dispose()
        {
            this.client.Dispose();
        }
    }
}
