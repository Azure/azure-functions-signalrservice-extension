using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

[assembly:InternalsVisibleTo("SignalRServiceExtension.Tests")]
namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal class AzureSignalRClient : IAzureSignalRClient
    {
        private readonly HttpClient httpClient;

        internal string BaseEndpoint { get; }
        internal string AccessKey { get; }

        internal AzureSignalRClient(string connectionString, HttpClient httpClient)
        {
            (BaseEndpoint, AccessKey) = ParseConnectionString(connectionString);
            this.httpClient = httpClient;
        }

        internal SignalRConnectionInfo GetClientConnectionInfo(string hubName)
        {
            var hubUrl = $"{BaseEndpoint}:5001/client/?hub={hubName}";
            var token = GenerateJwtBearer(null, hubUrl, null, DateTime.UtcNow.AddMinutes(30), AccessKey);
            return new SignalRConnectionInfo
            {
                Endpoint = hubUrl,
                AccessKey = token
            };
        }

        internal SignalRConnectionInfo GetServerConnectionInfo(string hubName)
        {
            var hubUrl = $"{BaseEndpoint}:5002/api/v1-preview/hub/{hubName}";
            var token = GenerateJwtBearer(null, hubUrl, null, DateTime.UtcNow.AddMinutes(30), AccessKey);
            return new SignalRConnectionInfo
            {
                Endpoint = hubUrl,
                AccessKey = token
            };
        }

        public Task SendMessage(string hubName, SignalRMessage message)
        {
            var connectionInfo = GetServerConnectionInfo(hubName);
            return PostJsonAsync(connectionInfo.Endpoint, message, connectionInfo.AccessKey);
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

        private Task<HttpResponseMessage> PostJsonAsync(string url, object body, string bearer)
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(url)
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.AcceptCharset.Clear();
            request.Headers.AcceptCharset.Add(new StringWithQualityHeaderValue("UTF-8"));

            var content = JsonConvert.SerializeObject(body);
            request.Content = new StringContent(content, Encoding.UTF8, "application/json");
            return httpClient.SendAsync(request);
        }
    }
}