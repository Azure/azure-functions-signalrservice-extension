﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Newtonsoft.Json;
using SignalRServiceExtension.Tests.Utils;
using Xunit;

namespace SignalRServiceExtension.Tests
{
    public class AzureSignalRClientTests
    {
        [Fact]
        public void AzureSignalRClient_ParsesConnectionString()
        {
            var azureSignalR = new AzureSignalRClient("Endpoint=https://foo.service.signalr.net;AccessKey=/abcdefghijklmnopqrstu/v/wxyz11111111111111=;Version=1.0;", null);
            Assert.Equal("https://foo.service.signalr.net", azureSignalR.BaseEndpoint);
            Assert.Equal("/abcdefghijklmnopqrstu/v/wxyz11111111111111=", azureSignalR.AccessKey);
            Assert.Equal("1.0", azureSignalR.Version);
        }

        [Fact]
        public void AzureSignalRClient_GetClientConnectionInfo_ReturnsValidInfo()
        {
            var azureSignalR = new AzureSignalRClient("Endpoint=https://foo.service.signalr.net;AccessKey=/abcdefghijklmnopqrstu/v/wxyz11111111111111=;Version=1.0;", null);

            var info = azureSignalR.GetClientConnectionInfo("chat");

            const string expectedUrl = "https://foo.service.signalr.net/client/?hub=chat";
            TestHelpers.EnsureValidAccessToken(
                audience: expectedUrl,
                signingKey: "/abcdefghijklmnopqrstu/v/wxyz11111111111111=", 
                accessToken: info.AccessToken);
            Assert.Equal(expectedUrl, info.Url);
        }

        [Fact]
        public void AzureSignalRClient_GetClientConnectionInfoWithUserId_ReturnsValidInfoWithUserId()
        {
            var azureSignalR = new AzureSignalRClient("Endpoint=https://foo.service.signalr.net;AccessKey=/abcdefghijklmnopqrstu/v/wxyz11111111111111=;Version=1.0;", null);
            var claims = new []
            {
                new Claim(ClaimTypes.NameIdentifier, "foo")
            };

            var info = azureSignalR.GetClientConnectionInfo("chat", claims);

            const string expectedEndpoint = "https://foo.service.signalr.net/client/?hub=chat";
            var claimsPrincipal = TestHelpers.EnsureValidAccessToken(
                audience: expectedEndpoint,
                signingKey: "/abcdefghijklmnopqrstu/v/wxyz11111111111111=", 
                accessToken: info.AccessToken);
            Assert.Contains(claimsPrincipal.Claims, 
                c => c.Type == ClaimTypes.NameIdentifier && c.Value == "foo");
            Assert.Equal(expectedEndpoint, info.Url);
        }

        [Fact]
        public void AzureSignalRClient_GetServerConnectionInfo_ReturnsValidInfo()
        {
            var azureSignalR = new AzureSignalRClient("Endpoint=https://foo.service.signalr.net;AccessKey=/abcdefghijklmnopqrstu/v/wxyz11111111111111=;Version=1.0;", null);

            var info = azureSignalR.GetServerConnectionInfo("chat");

            const string expectedUrl = "https://foo.service.signalr.net/api/v1/hubs/chat";
            TestHelpers.EnsureValidAccessToken(
                audience: expectedUrl,
                signingKey: "/abcdefghijklmnopqrstu/v/wxyz11111111111111=", 
                accessToken: info.AccessToken);
            Assert.Equal(expectedUrl, info.Url);
        }

        [Fact]
        public async Task SendToAll_CallsAzureSignalRService()
        {
            var connectionString = "Endpoint=https://foo.service.signalr.net;AccessKey=/abcdefghijklmnopqrstu/v/wxyz11111111111111=;Version=1.0;";
            var hubName = "chat";
            var requestHandler = new FakeHttpMessageHandler();
            var httpClient = new HttpClient(requestHandler);
            var azureSignalR = new AzureSignalRClient(connectionString, httpClient);

            await azureSignalR.SendToAll(hubName, new SignalRData
            {
                Target = "newMessage",
                Arguments = new object[] { "arg1", "arg2" }
            });

            const string expectedEndpoint = "https://foo.service.signalr.net/api/v1/hubs/chat";
            var request = requestHandler.HttpRequestMessage;
            Assert.Equal("application/json", request.Content.Headers.ContentType.MediaType);
            Assert.Equal(expectedEndpoint, request.RequestUri.AbsoluteUri);

            var actualRequestBody = JsonConvert.DeserializeObject<SignalRMessage>(await request.Content.ReadAsStringAsync());
            Assert.Equal("newMessage", actualRequestBody.Target);
            Assert.Equal("arg1", actualRequestBody.Arguments[0]);
            Assert.Equal("arg2", actualRequestBody.Arguments[1]);

            var authorizationHeader = request.Headers.Authorization;
            Assert.Equal("Bearer", authorizationHeader.Scheme);
            TestHelpers.EnsureValidAccessToken(
                audience: expectedEndpoint,
                signingKey: "/abcdefghijklmnopqrstu/v/wxyz11111111111111=", 
                accessToken: authorizationHeader.Parameter);
        }

        [Fact]
        public async Task SendToUser_CallsAzureSignalRService()
        {
            var connectionString = "Endpoint=https://foo.service.signalr.net;AccessKey=/abcdefghijklmnopqrstu/v/wxyz11111111111111=;Version=1.0;";
            var hubName = "chat";
            var requestHandler = new FakeHttpMessageHandler();
            var httpClient = new HttpClient(requestHandler);
            var azureSignalR = new AzureSignalRClient(connectionString, httpClient);

            await azureSignalR.SendToUser(
                hubName, 
                "userId1",
                new SignalRData
                {
                    Target = "newMessage",
                    Arguments = new object[] { "arg1", "arg2" }
                });

            var baseEndpoint = "https://foo.service.signalr.net/api/v1/hubs/chat";
            var expectedEndpoint = $"{baseEndpoint}/users/userId1";
            var request = requestHandler.HttpRequestMessage;
            Assert.Equal("application/json", request.Content.Headers.ContentType.MediaType);
            Assert.Equal(expectedEndpoint, request.RequestUri.AbsoluteUri);

            var actualRequestBody = JsonConvert.DeserializeObject<SignalRMessage>(await request.Content.ReadAsStringAsync());
            Assert.Equal("newMessage", actualRequestBody.Target);
            Assert.Equal("arg1", actualRequestBody.Arguments[0]);
            Assert.Equal("arg2", actualRequestBody.Arguments[1]);

            var authorizationHeader = request.Headers.Authorization;
            Assert.Equal("Bearer", authorizationHeader.Scheme);
            TestHelpers.EnsureValidAccessToken(
                audience: expectedEndpoint,
                signingKey: "/abcdefghijklmnopqrstu/v/wxyz11111111111111=", 
                accessToken: authorizationHeader.Parameter);
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