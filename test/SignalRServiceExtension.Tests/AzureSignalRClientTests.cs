using System;
using System.Text;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using SignalRServiceExtension.Tests.Utils;
using Xunit;

namespace SignalRServiceExtension.Tests
{
    public class AzureSignalRClientTests
    {
        [Fact]
        public void AzureSignalRClient_ParsesConnectionString()
        {
            var azureSignalR = new AzureSignalRClient("Endpoint=https://foo.service.signalr.net;AccessKey=/abcdefghijklmnopqrstu/v/wxyz11111111111111=;");
            Assert.Equal("https://foo.service.signalr.net", azureSignalR.BaseEndpoint);
            Assert.Equal("/abcdefghijklmnopqrstu/v/wxyz11111111111111=", azureSignalR.AccessKey);
        }

        [Fact]
        public void AzureSignalRClient_GetClientConnectionInfo_ReturnsValidInfo()
        {
            var azureSignalR = new AzureSignalRClient("Endpoint=https://foo.service.signalr.net;AccessKey=/abcdefghijklmnopqrstu/v/wxyz11111111111111=;");

            var info = azureSignalR.GetClientConnectionInfo("chat");

            const string expectedEndpoint = "https://foo.service.signalr.net:5001/client/?hub=chat";
            TestHelpers.EnsureValidAccessKey(
                audience: expectedEndpoint,
                signingKey: "/abcdefghijklmnopqrstu/v/wxyz11111111111111=", 
                accessKey: info.AccessKey);
            Assert.Equal(expectedEndpoint, info.Endpoint);
        }

        [Fact]
        public void AzureSignalRClient_GetServerConnectionInfo_ReturnsValidInfo()
        {
            var azureSignalR = new AzureSignalRClient("Endpoint=https://foo.service.signalr.net;AccessKey=/abcdefghijklmnopqrstu/v/wxyz11111111111111=;");

            var info = azureSignalR.GetServerConnectionInfo("chat");

            const string expectedEndpoint = "https://foo.service.signalr.net:5002/api/v1-preview/hub/chat";
            TestHelpers.EnsureValidAccessKey(
                audience: expectedEndpoint,
                signingKey: "/abcdefghijklmnopqrstu/v/wxyz11111111111111=", 
                accessKey: info.AccessKey);
            Assert.Equal(expectedEndpoint, info.Endpoint);
        }
    }
}