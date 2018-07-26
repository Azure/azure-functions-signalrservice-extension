using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Xunit;

namespace SignalRServiceExtension.Tests
{
    public class SignalRMessageAsyncCollectorTests
    {
        [Fact]
        public async Task Works()
        {
            var attr = new SignalRAttribute
            {
                ConnectionStringSetting = "Endpoint=https://foo.service.signalr.net;AccessKey=/abcdefghijklmnopqrstu/v/wxyz11111111111111=;",
                HubName = "chat"
            };
            var requestHandler = new FakeHttpMessageHandler();
            var httpClient = new HttpClient(requestHandler);
            var collector = new SignalRMessageAsyncCollector(attr, httpClient);

            await collector.AddAsync(new SignalRMessage
            {
                Target = "newMessage",
                Arguments = new object[] { "arg1", "arg2" }
            });

            var actualRequestBody = JsonConvert.DeserializeObject<SignalRMessage>(await requestHandler.HttpRequestMessage.Content.ReadAsStringAsync());
            Assert.Equal("newMessage", actualRequestBody.Target);
            Assert.Equal("arg1", actualRequestBody.Arguments[0]);
            Assert.Equal("arg2", actualRequestBody.Arguments[1]);
        }

        private class FakeHttpMessageHandler : HttpMessageHandler
        {
            public HttpRequestMessage HttpRequestMessage { get; private set; }
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                HttpRequestMessage = request;
                var response = new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable);
                response.Content = new StringContent("", Encoding.UTF8, "application/json");
                return Task.FromResult(response);
            }
        }
    }
}