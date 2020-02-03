// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    /// <summary>
    /// Validates a incoming request and extracts any <see cref="ClaimsPrincipal"/> contained within the bearer token.
    /// </summary>
    //  todo: add log
    internal class DefaultAccessTokenProvider : IAccessTokenProvider
    {
        private const string AuthHeaderName = "Authorization";
        private const string BearerPrefix = "Bearer ";
        private readonly TokenValidationParameters tokenValidationParameters = new TokenValidationParameters();
        private readonly JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();

        public DefaultAccessTokenProvider(Action<TokenValidationParameters> configureTokenValidationParameters)
        {
            if (configureTokenValidationParameters == null)
            {
                throw new ArgumentNullException(nameof(configureTokenValidationParameters));
            }
            configureTokenValidationParameters.Invoke(tokenValidationParameters);
        }

        public AccessTokenResult ValidateToken(HttpRequest request)
        {
            try
            {
                // Get the token from the header
                if (request != null &&
                    request.Headers.ContainsKey(AuthHeaderName) &&
                    request.Headers[AuthHeaderName].ToString().StartsWith(BearerPrefix))
                {
                    var token = request.Headers[AuthHeaderName].ToString().Substring(BearerPrefix.Length);
                    // Validate the token
                    var result = handler.ValidateToken(token, tokenValidationParameters, out _);
                    return AccessTokenResult.Success(result);
                }

                return AccessTokenResult.NoToken();
            }
            catch (SecurityTokenExpiredException ex)
            {
                return AccessTokenResult.Expired(ex);
            }
            catch (Exception ex)
            {
                return AccessTokenResult.Error(ex);
            }
        }
    }
}