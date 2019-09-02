// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Azure.SignalR.Management;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    /// <summary>
    /// AzureSignalRClient used for negotiation, pulishing messages and managing group membership.
    /// It will be created for each function request.
    /// </summary>
    internal class AzureSignalRClient : IAzureSignalRSender
    {
        public const string AzureSignalRUserPrefix = "asrs.u.";
        private static readonly string[] SystemClaims =
        {
            "aud", // Audience claim, used by service to make sure token is matched with target resource.
            "exp", // Expiration time claims. A token is valid only before its expiration time.
            "iat", // Issued At claim. Added by default. It is not validated by service.
            "nbf"  // Not Before claim. Added by default. It is not validated by service.
        };
        private readonly IServiceHubContextStore serviceHubContextStore;
        private readonly IServiceManager serviceManager;
        private readonly string hubName;

        internal AzureSignalRClient(IServiceHubContextStore serviceHubContextStore, string hubName)
        {
            this.serviceHubContextStore = serviceHubContextStore;
            this.serviceManager = serviceHubContextStore.ServiceManager;
            this.hubName = hubName;
        }

        public SignalRConnectionInfo GetClientConnectionInfo(string userId, string idToken, string[] claimTypeList)
        {
            IEnumerable<Claim> customerClaims = null;
            if (idToken != null && claimTypeList != null && claimTypeList.Length > 0)
            {
                var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(idToken);
                customerClaims = from claim in jwtToken.Claims
                                 where claimTypeList.Contains(claim.Type)
                                 select claim;
            }

            return new SignalRConnectionInfo
            {
                Url = serviceManager.GetClientEndpoint(hubName),
                AccessToken = serviceManager.GenerateClientAccessToken(
                    hubName, userId, BuildJwtClaims(customerClaims, AzureSignalRUserPrefix).ToList())
            };
        }

        public async Task SendToAll(SignalRData data)
        {
            var serviceHubContext = await serviceHubContextStore.GetOrAddAsync(hubName);
            await serviceHubContext.Clients.All.SendCoreAsync(data.Target, data.Arguments);
        }

        public async Task SendToConnection(string connectionId, SignalRData data)
        {
            var serviceHubContext = await serviceHubContextStore.GetOrAddAsync(hubName);
            await serviceHubContext.Clients.Client(connectionId).SendCoreAsync(data.Target, data.Arguments);
        }

        public async Task SendToUser(string userId, SignalRData data)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException($"{nameof(userId)} cannot be null or empty");
            }
            var serviceHubContext = await serviceHubContextStore.GetOrAddAsync(hubName);
            await serviceHubContext.Clients.User(userId).SendCoreAsync(data.Target, data.Arguments);
        }

        public async Task SendToGroup(string groupName, SignalRData data)
        {
            if (string.IsNullOrEmpty(groupName))
            {
                throw new ArgumentException($"{nameof(groupName)} cannot be null or empty");
            }
            var serviceHubContext = await serviceHubContextStore.GetOrAddAsync(hubName);
            await serviceHubContext.Clients.Group(groupName).SendCoreAsync(data.Target, data.Arguments);
        }

        public async Task AddUserToGroup(string userId, string groupName)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException($"{nameof(userId)} cannot be null or empty");
            }
            if (string.IsNullOrEmpty(groupName))
            {
                throw new ArgumentException($"{nameof(groupName)} cannot be null or empty");
            }
            var serviceHubContext = await serviceHubContextStore.GetOrAddAsync(hubName);
            await serviceHubContext.UserGroups.AddToGroupAsync(userId, groupName);
        }

        public async Task RemoveUserFromGroup(string userId, string groupName)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException($"{nameof(userId)} cannot be null or empty");
            }
            if (string.IsNullOrEmpty(groupName))
            {
                throw new ArgumentException($"{nameof(groupName)} cannot be null or empty");
            }
            var serviceHubContext = await serviceHubContextStore.GetOrAddAsync(hubName);
            await serviceHubContext.UserGroups.RemoveFromGroupAsync(userId, groupName);
        }

        public async Task AddConnectionToGroup(string connectionId, string groupName)
        {
            if (string.IsNullOrEmpty(connectionId))
            {
                throw new ArgumentException($"{nameof(connectionId)} cannot be null or empty");
            }
            if (string.IsNullOrEmpty(groupName))
            {
                throw new ArgumentException($"{nameof(groupName)} cannot be null or empty");
            }
            var serviceHubContext = await serviceHubContextStore.GetOrAddAsync(hubName);
            await serviceHubContext.Groups.AddToGroupAsync(connectionId, groupName);
        }

        public async Task RemoveConnectionFromGroup(string connectionId, string groupName)
        {
            if (string.IsNullOrEmpty(connectionId))
            {
                throw new ArgumentException($"{nameof(connectionId)} cannot be null or empty");
            }
            if (string.IsNullOrEmpty(groupName))
            {
                throw new ArgumentException($"{nameof(groupName)} cannot be null or empty");
            }
            var serviceHubContext = await serviceHubContextStore.GetOrAddAsync(hubName);
            await serviceHubContext.Groups.RemoveFromGroupAsync(connectionId, groupName);
        }

        private static IEnumerable<Claim> BuildJwtClaims(IEnumerable<Claim> customerClaims, string prefix)
        {
            if (customerClaims != null)
            {
                foreach (var claim in customerClaims)
                {
                    // Add AzureSignalRUserPrefix if customer's claim name is duplicated with SignalR system claims.
                    // And split it when return from SignalR Service.
                    if (SystemClaims.Contains(claim.Type))
                    {
                        yield return new Claim(prefix + claim.Type, claim.Value);
                    }
                    else
                    {
                        yield return claim;
                    }
                }
            }
        }
    }
}