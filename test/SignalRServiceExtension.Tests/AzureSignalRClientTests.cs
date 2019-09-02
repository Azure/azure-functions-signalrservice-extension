// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Xunit;

namespace SignalRServiceExtension.Tests
{
    public class AzureSignalRClientTests
    {
        [Fact]
        public void GetClientConnectionInfo()
        {
            var hubName = "TestHub";
            var hubUrl = "http://localhost";
            var accessKey = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var connectionString = $"Endpoint={hubUrl};AccessKey={accessKey};Version=1.0;";
            var userId = "User";
            var idToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
            var expectedName = "John Doe";
            var expectedIat = "1516239022";
            var claimTypeList = new string[] { "name", "iat" };
            var serviceManager = new ServiceManagerBuilder()
                .WithOptions(o =>
                {
                    o.ConnectionString = connectionString;
                })
                .Build();
            var serviceHubContextStore = new ServiceHubContextStore(serviceManager, null);
            var azureSignalRClient = new AzureSignalRClient(serviceHubContextStore, hubName);
            var connectionInfo = azureSignalRClient.GetClientConnectionInfo(userId, idToken, claimTypeList);

            Assert.Equal(connectionInfo.Url, $"{hubUrl}/client/?hub={hubName.ToLower()}");

            var claims = new JwtSecurityTokenHandler().ReadJwtToken(connectionInfo.AccessToken).Claims;
            Assert.Equal(expectedName, GetClaimValue(claims, "name"));
            Assert.Equal(expectedIat, GetClaimValue(claims, $"{AzureSignalRClient.AzureSignalRUserPrefix}iat"));
        }

        private string GetClaimValue(IEnumerable<Claim> claims, string type) =>
            (from c in claims
             where c.Type == type
             select c.Value).FirstOrDefault();
    }
}