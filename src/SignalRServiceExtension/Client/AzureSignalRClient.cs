// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal class AzureSignalRClient : IAzureSignalRSender
    {
        private readonly HttpClient httpClient;

        internal string BaseEndpoint { get; }
        internal string AccessKey { get; }

        internal AzureSignalRClient(string connectionString, HttpClient httpClient)
        {
            (BaseEndpoint, AccessKey) = ParseConnectionString(connectionString);
            this.httpClient = httpClient;
        }

        internal SignalRConnectionInfo GetClientConnectionInfo(string hubName, IEnumerable<Claim> claims = null)
        {
            var hubUrl = $"{BaseEndpoint}:5001/client/?hub={hubName}";
            var identity = new ClaimsIdentity(claims);
            var token = GenerateJwtBearer(null, hubUrl, identity, DateTime.UtcNow.AddMinutes(30), AccessKey);
            return new SignalRConnectionInfo
            {
                Url = hubUrl,
                AccessToken = token
            };
        }

        internal SignalRConnectionInfo GetServerConnectionInfo(string hubName, string additionalPath = "")
        {
            var hubUrl = $"{BaseEndpoint}/api/v1/hubs/{hubName}";
            var audienceUrl = $"{hubUrl}{additionalPath}";
            var token = GenerateJwtBearer(null, audienceUrl, null, DateTime.UtcNow.AddMinutes(30), AccessKey);
            return new SignalRConnectionInfo
            {
                Url = hubUrl,
                AccessToken = token
            };
        }

        public Task SendToAll(string hubName, SignalRData data)
        {
            var connectionInfo = GetServerConnectionInfo(hubName);
            return PostJsonAsync(connectionInfo.Url, data, connectionInfo.AccessToken);
        }

        public Task SendToUser(string hubName, string userId, SignalRData data)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException($"{nameof(userId)} cannot be null or empty");
            }

            var userIdsSegment = $"/users/{userId}";
            var connectionInfo = GetServerConnectionInfo(hubName, userIdsSegment);
            var uri = $"{connectionInfo.Url}{userIdsSegment}";
            return PostJsonAsync(uri, data, connectionInfo.AccessToken);
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