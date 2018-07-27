using System;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace SignalRServiceExtension.Tests.Utils
{
    class TestHelpers
    {
        internal static void EnsureValidAccessKey(string audience, string signingKey, string accessKey)
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