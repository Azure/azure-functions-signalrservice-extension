// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace SignalRServiceExtension.Tests
{
    public class DefaultAccessTokenProviderTests
    {
        public static IEnumerable<object[]> TestData = new List<object[]>
        {
            new object []
            {
                "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1lIjoiYWFhIiwiZXhwIjoxNjk5ODE5MDI1fQ.joh9CXSfRpgZhoraozdQ0Z1DxmUhlXF4ENt_1Ttz7x8",
                AccessTokenStatus.Valid
            },
            new object[]
            {
                "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1lIjoiYWFhIiwiZXhwIjoyNTMwODk4OTIyMjV9.1dbS2bgRrTvxHhph9lh0TLw34a46ts5jwaJH0OeS8-s",
                AccessTokenStatus.Error
            },
            new object[]
            {
                "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1lIjoiYWFhIiwiZXhwIjo5NDk4MDkwMjV9.7LFRqmpdkINvdn39pb-pUxz_VGUP94Q5CS9YuL1_gVI",
                AccessTokenStatus.Expired
            },
            new object[]
            {
                "",
                AccessTokenStatus.Empty
            }

        };

        [Theory]
        [MemberData(nameof(TestData))]
        public void ValidateAccessTokenFacts(string tokenString, AccessTokenStatus expectedStatus)
        {
            var ctx = new DefaultHttpContext();
            var req = new DefaultHttpRequest(ctx);
            req.Headers.Add("Authorization", new StringValues(tokenString));

            var issuerToken = "bXlmdW5jdGlvbmF1dGh0ZXN0"; // base64 encoded for "myfunctionauthtest";
            Action<TokenValidationParameters> configureTokenValidationParameters = parameters =>
            {
                parameters.IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(issuerToken));
                parameters.RequireSignedTokens = true;
                parameters.ValidateAudience = false;
                parameters.ValidateIssuer = false;
                parameters.ValidateIssuerSigningKey = true;
                parameters.ValidateLifetime = true;
            };

            var accessTokenProvider = new DefaultAccessTokenProvider(configureTokenValidationParameters);
            var accessTokenResult = accessTokenProvider.ValidateToken(req);

            Assert.Equal(expectedStatus, accessTokenResult.Status);
        }
    }
}
