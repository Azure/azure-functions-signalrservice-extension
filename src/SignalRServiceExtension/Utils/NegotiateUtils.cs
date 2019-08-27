using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal static class NegotiateUtils
    {
        private const string DefaultAuthenticationType = "Bearer";
        private static readonly ClaimsIdentity DefaultClaimsIdentity = new ClaimsIdentity();
        private static readonly ClaimsPrincipal EmptyPrincipal = new ClaimsPrincipal(DefaultClaimsIdentity);
        private static readonly string DefaultNameClaimType = DefaultClaimsIdentity.NameClaimType;
        private static readonly string DefaultRoleClaimType = DefaultClaimsIdentity.RoleClaimType;

        /// <summary>
        /// Get User Id from negotiate request.
        /// Try get from claims and then from header **x-ms-client-principal-name**
        /// </summary>
        public static string GetUserId(HttpRequestMessage req)
        {
            var httpContext = req.Properties["HttpContext"] as HttpContext;
            var userId = httpContext?.User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

            if (string.IsNullOrEmpty(userId) &&
                req.Headers.TryGetValues("x-ms-signalr-userid", out var values))
            {
                userId = values.FirstOrDefault();
            }

            return userId;
        }

        public static Dictionary<string, string> GetClaims(HttpRequestMessage req)
        {
            var httpContext = req.Properties["HttpContext"] as HttpContext;
            var claims = GetClaims(req, httpContext?.User, GetUserId(req));

            var claimDictionary = new Dictionary<string, string>();
            foreach (var claim in claims)
            {
                claimDictionary[claim.Type] = claim.Value;
            }

            return claimDictionary;
        }

        // This method keep highly constant with Azure SignalR SDK
        private static IEnumerable<Claim> GetClaims(HttpRequestMessage req, ClaimsPrincipal user, string userId)
        {
            if (!string.IsNullOrEmpty(userId))
            {
                yield return new Claim(ClaimType.UserId, userId);
            }

            var authenticationType = user?.Identity?.AuthenticationType;

            // No need to pass it when the authentication type is Bearer
            if (authenticationType != null && authenticationType != DefaultAuthenticationType)
            {
                yield return new Claim(ClaimType.AuthenticationType, authenticationType);
            }

            if (user?.Identity is ClaimsIdentity identity)
            {
                var nameType = identity.NameClaimType;
                if (nameType != null && nameType != DefaultNameClaimType)
                {
                    yield return new Claim(ClaimType.NameType, nameType);
                }

                var roleType = identity.RoleClaimType;
                if (roleType != null && roleType != DefaultRoleClaimType)
                {
                    yield return new Claim(ClaimType.RoleType, roleType);
                }
            }
        }
    }
}
