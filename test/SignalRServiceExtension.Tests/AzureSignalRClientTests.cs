// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Extensions.Logging;
using SignalRServiceExtension.Tests.Utils;
using Xunit;

namespace SignalRServiceExtension.Tests
{
    public class AzureSignalRClientTests
    {
        private const string BaseEndpoint = "https://foo.service.signalr.net";
        private const string AccessKey = "/abcdefghijklmnopqrstu/v/wxyz11111111111111=";
        private static readonly string ConnectionString = $"Endpoint={BaseEndpoint};AccessKey={AccessKey};Version=1.0;";
        private const string HubName = "hub";
        private const string UserId = "user";

        private static string ExpectClientUrl
        {
            get { return $"{BaseEndpoint}/client/?hub={HubName}"; }
        }

        [Fact]
        public async Task AzureSignalRClient_GetClientConnectionInfo_ReturnsValidInfo()
        {
            var serviceManager = new ServiceManagerBuilder()
               .WithOptions(o =>
               {
                   o.ConnectionString = ConnectionString;
               })
               .Build();

            using (var loggerFactory = new LoggerFactory())
            {
                var serviceHubContext = await serviceManager.CreateHubContextAsync(HubName, loggerFactory);
                var serviceHubContextStore = new ServiceHubContextStore(serviceManager, loggerFactory);
                var azureSignalR = new AzureSignalRClient(serviceHubContextStore, serviceManager);
                var info = azureSignalR.GetClientConnectionInfo(HubName, null);

                string expectedUrl = ExpectClientUrl;
                TestHelpers.EnsureValidAccessToken(
                    audience: expectedUrl,
                    signingKey: AccessKey,
                    accessToken: info.AccessToken);
                Assert.Equal(expectedUrl, info.Url);
            }
        }

        [Fact]
        public async Task AzureSignalRClient_GetClientConnectionInfoWithUserId_ReturnsValidInfoWithUserId()
        {

            var serviceManager = new ServiceManagerBuilder()
                .WithOptions(o =>
                {
                    o.ConnectionString = ConnectionString;
                })
                .Build();

            using (var loggerFactory = new LoggerFactory())
            {
                var serviceHubContext = await serviceManager.CreateHubContextAsync(HubName, loggerFactory);
                var serviceHubContextStore = new ServiceHubContextStore(serviceManager, loggerFactory);
                var azureSignalR = new AzureSignalRClient(serviceHubContextStore, serviceManager);
                var info = azureSignalR.GetClientConnectionInfo(HubName, UserId);
                var claimsPrincipal = TestHelpers.EnsureValidAccessToken(
                    audience: ExpectClientUrl,
                    signingKey: AccessKey,
                    accessToken: info.AccessToken);
                Assert.Contains(claimsPrincipal.Claims,
                c => c.Type == ClaimTypes.NameIdentifier && c.Value == UserId);
                Assert.Equal(ExpectClientUrl, info.Url);
            }
        }
    }
}