using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal class AzureSignalR
    {
        internal string BaseEndpoint { get; }
        internal string AccessKey { get; }

        internal AzureSignalR(string connectionString)
        {
            (BaseEndpoint, AccessKey) = ParseConnectionString(connectionString);
        }

        internal AzureSignalRConnectionInfo GetClientConnectionInfo(string hubName)
        {
            var hubUrl = $"{BaseEndpoint}:5001/client/?hub={hubName}";
            var token = GenerateJwtBearer(null, hubUrl, null, DateTime.UtcNow.AddMinutes(30), AccessKey);
            return new AzureSignalRConnectionInfo
            {
                Endpoint = hubUrl,
                AccessKey = token
            };
        }

        internal AzureSignalRConnectionInfo GetServerConnectionInfo(string hubName)
        {
            var hubUrl = $"{BaseEndpoint}:5002/api/v1-preview/hub/{hubName}";
            var token = GenerateJwtBearer(null, hubUrl, null, DateTime.UtcNow.AddMinutes(30), AccessKey);
            return new AzureSignalRConnectionInfo
            {
                Endpoint = hubUrl,
                AccessKey = token
            };
        }

        private (string EndPoint, string AccessKey) ParseConnectionString(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentException("SignalR Service connection string is empty");
            }

            var endpointMatch = Regex.Match(connectionString, @"endpoint=([^;]+)", RegexOptions.IgnoreCase);
            if (!endpointMatch.Success)
            {
                throw new ArgumentException("No endpoint present in SignalR Service connection string");
            }
            var accessKeyMatch = Regex.Match(connectionString, @"accesskey=([^;]+)", RegexOptions.IgnoreCase);
            if (!accessKeyMatch.Success)
            {
                throw new ArgumentException("No access key present in SignalR Service connection string");
            }

            return (endpointMatch.Groups[1].Value, accessKeyMatch.Groups[1].Value);
        }

        private string GenerateJwtBearer(string issuer, string audience, ClaimsIdentity subject, DateTime? expires, string signingKey)
        {
            SigningCredentials credentials = null;
            if (!string.IsNullOrEmpty(signingKey))
            {
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
                credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            }
            var jwtTokenHandler = new JwtSecurityTokenHandler();
            var token = jwtTokenHandler.CreateJwtSecurityToken(
                issuer: issuer,
                audience: audience,
                subject: subject,
                expires: expires,
                signingCredentials: credentials);
            return jwtTokenHandler.WriteToken(token);
        }
    }
}