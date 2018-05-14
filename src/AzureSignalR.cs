using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    public class AzureSignalR
    {
        public string BaseEndpoint { get; }
        public string AccessKey { get; }

        public AzureSignalR(string connectionString)
        {
            (BaseEndpoint, AccessKey) = ParseConnectionString(connectionString);
        }

        public AzureSignalREndpoint GetClientEndpoint(string hubName)
        {
            var hubUrl = $"{BaseEndpoint}:5001/client/?hub={hubName}";
            var token = GenerateJwtBearer(null, hubUrl, null, DateTime.UtcNow.AddMinutes(30), AccessKey);
            return new AzureSignalREndpoint
            {
                Endpoint = hubUrl,
                AccessKey = token
            };
        }

        public AzureSignalREndpoint GetServerEndpoint(string hubName)
        {
            var hubUrl = $"{BaseEndpoint}:5002/api/v1-preview/hub/{hubName}";
            var token = GenerateJwtBearer(null, hubUrl, null, DateTime.UtcNow.AddMinutes(30), AccessKey);
            return new AzureSignalREndpoint
            {
                Endpoint = hubUrl,
                AccessKey = token
            };
        }

        private (string EndPoint, string AccessKey) ParseConnectionString(string connectionString)
        {
            var endpointMatch = Regex.Match(connectionString, @"endpoint=([^;]+)", RegexOptions.IgnoreCase);
            if (!endpointMatch.Success)
            {
                throw new ArgumentException("No endpoint present in connection string");
            }
            var accessKeyMatch = Regex.Match(connectionString, @"accesskey=([^;]+)", RegexOptions.IgnoreCase);
            if (!accessKeyMatch.Success)
            {
                throw new ArgumentException("No access key present in connection string");
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