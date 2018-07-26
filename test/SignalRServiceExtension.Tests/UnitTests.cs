using System;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace SignalRServiceExtension.Tests
{
    public class UnitTests
    {
        [Fact]
        public void AzureSignalR_ParsesConnectionString()
        {
            var azureSignalR = new AzureSignalR("Endpoint=https://foo.service.signalr.net;AccessKey=/abcdefghijklmnopqrstu/v/wxyz11111111111111=;");
            Assert.Equal("https://foo.service.signalr.net", azureSignalR.BaseEndpoint);
            Assert.Equal("/abcdefghijklmnopqrstu/v/wxyz11111111111111=", azureSignalR.AccessKey);
        }

        [Fact]
        public void AzureSignalR_GetClientConnectionInfo_ReturnsValidInfo()
        {
            var azureSignalR = new AzureSignalR("Endpoint=https://foo.service.signalr.net;AccessKey=/abcdefghijklmnopqrstu/v/wxyz11111111111111=;");

            var info = azureSignalR.GetClientConnectionInfo("chat");

            const string expectedEndpoint = "https://foo.service.signalr.net:5001/client/?hub=chat";
            EnsureValidAccessKey(
                audience: expectedEndpoint,
                signingKey: "/abcdefghijklmnopqrstu/v/wxyz11111111111111=", 
                accessKey: info.AccessKey);
            Assert.Equal(expectedEndpoint, info.Endpoint);
        }

        [Fact]
        public void AzureSignalR_GetServerConnectionInfo_ReturnsValidInfo()
        {
            var azureSignalR = new AzureSignalR("Endpoint=https://foo.service.signalr.net;AccessKey=/abcdefghijklmnopqrstu/v/wxyz11111111111111=;");

            var info = azureSignalR.GetServerConnectionInfo("chat");

            const string expectedEndpoint = "https://foo.service.signalr.net:5002/api/v1-preview/hub/chat";
            EnsureValidAccessKey(
                audience: expectedEndpoint,
                signingKey: "/abcdefghijklmnopqrstu/v/wxyz11111111111111=", 
                accessKey: info.AccessKey);
            Assert.Equal(expectedEndpoint, info.Endpoint);
        }

        private void EnsureValidAccessKey(string audience, string signingKey, string accessKey)
        {
            var validationParameters =
                new TokenValidationParameters
                {
                    ValidAudiences = new[] { audience },
                    ValidateAudience = true,
                    ValidateIssuer = false,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                    RequireSignedTokens = true,
                    RequireExpirationTime = true,
                    ValidateLifetime = true,
                    LifetimeValidator = (_, expires, __, ___) => 
                        expires.HasValue && expires > DateTime.Now.AddMinutes(5) // at least 5 minutes
                };
            var handler = new JwtSecurityTokenHandler();
            handler.ValidateToken(accessKey, validationParameters, out _);
        }
    }
}