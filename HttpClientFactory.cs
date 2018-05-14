using System;
using System.Net.Http;

namespace SignalRExtension
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