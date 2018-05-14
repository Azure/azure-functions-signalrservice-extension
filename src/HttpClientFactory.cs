using System;
using System.Net.Http;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    public static class HttpClientFactory
    {
        private static Lazy<HttpClient> httpClient = new Lazy<HttpClient>(() => new HttpClient());
        public static HttpClient GetInstance()
        {
            return httpClient.Value;
        }
    }
}