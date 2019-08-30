using System;
using System.Collections;
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
        private static readonly string[] SystemClaims =
        {
            "aud", // Audience claim, used by service to make sure token is matched with target resource.
            "exp", // Expiration time claims. A token is valid only before its expiration time.
            "iat", // Issued At claim. Added by default. It is not validated by service.
            "nbf"  // Not Before claim. Added by default. It is not validated by service.
        };

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
            var httpContext = req.GetHttpContext();
            var userId = httpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId) &&
                req.Headers.TryGetValues("x-ms-client-principal-name", out var values))
            {
                userId = values.FirstOrDefault();
            }

            return userId;
        }

        public static Dictionary<string, string> GetClaims(HttpRequestMessage req)
        {
            var httpContext = req.GetHttpContext();
            var customerClaims = GetOriginalClaims(httpContext?.User).ToList();

            var claimDictionary = new Dictionary<string, string>();
            foreach (var claim in customerClaims)
            {
                claimDictionary[claim.Type] = claim.Value;
            }

            return claimDictionary;
        }

        public static IList<Claim> GetClaimsForJwtToken(HttpRequestMessage req, IList<Claim> customerClaims,
            string userId)
        {
            var httpContext = req.GetHttpContext();
            var systemClaims = GetSystemClaims(httpContext?.User, userId).ToList();
            var formattedCustomerClaims = customerClaims.Select(c =>
                SystemClaims.Contains(c.Type) ? new Claim(ClaimType.AzureSignalRUserPrefix + c.Type, c.Value) : c);
            systemClaims.AddRange(formattedCustomerClaims);
            return systemClaims;
        }

        private static IEnumerable<Claim> GetOriginalClaims(ClaimsPrincipal user)
        {
            return user?.Claims.Where(c => c.Type != ClaimTypes.NameIdentifier) ?? new List<Claim>();
        }

        // This method keep highly constant with Azure SignalR SDK
        private static IEnumerable<Claim> GetSystemClaims(ClaimsPrincipal user, string userId)
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

        private static HttpContext GetHttpContext(this HttpRequestMessage req)
        {
            if (req.Properties.TryGetValue("HttpContext", out var context))
            {
                return context as HttpContext;
            }
            return null;
        }
    }
}
