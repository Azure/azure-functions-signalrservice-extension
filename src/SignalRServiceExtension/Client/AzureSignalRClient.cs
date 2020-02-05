// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    /// <summary>
    /// AzureSignalRClient used for negotiation, publishing messages and managing group membership.
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
        private readonly IServiceManagerStore serviceManagerStore;
        private readonly string connectionString;

        public string HubName { get; }

        internal AzureSignalRClient(IServiceManagerStore serviceManagerStore, string connectionString, string hubName)
        {
            this.serviceManagerStore = serviceManagerStore;
            this.HubName = hubName;
            this.connectionString = connectionString;
        }

        public SignalRConnectionInfo GetClientConnectionInfo(string userId, string idToken, string[] claimTypeList)
        {
            var customerClaims = GetCustomerClaims(idToken, claimTypeList);
            var serviceManager = serviceManagerStore.GetOrAddByConnectionString(connectionString).ServiceManager;

            return new SignalRConnectionInfo
            {
                Url = serviceManager.GetClientEndpoint(HubName),
                AccessToken = serviceManager.GenerateClientAccessToken(
                    HubName, userId, BuildJwtClaims(customerClaims, AzureSignalRUserPrefix).ToList())
            };
        }

        public SignalRConnectionInfo GetClientConnectionInfo(string userId, IList<Claim> claims)
        {
            var serviceManager = serviceManagerStore.GetOrAddByConnectionString(connectionString).ServiceManager;
            return new SignalRConnectionInfo
            {
                Url = serviceManager.GetClientEndpoint(HubName),
                AccessToken = serviceManager.GenerateClientAccessToken(
                    HubName, userId, BuildJwtClaims(claims, AzureSignalRUserPrefix).ToList())
            };
        }

        public SignalRConnectionInfoV2 GetClientConnectionInfoV2(string userId, string idToken, string[] claimTypeList, AccessTokenResult accessTokenResult)
        {
            return new SignalRConnectionInfoV2(GetClientConnectionInfo(userId, idToken, claimTypeList), accessTokenResult);
        }

        public SignalRConnectionInfoV2 GetClientConnectionInfoV2(string userId, IList<Claim> claims, AccessTokenResult accessTokenResult)
        {
            return new SignalRConnectionInfoV2(GetClientConnectionInfo(userId, claims), accessTokenResult);
        }

        public IList<Claim> GetCustomerClaims(string idToken, string[] claimTypeList)
        {
            var customerClaims = new List<Claim>();

            if (idToken != null && claimTypeList != null && claimTypeList.Length > 0)
            {
                var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(idToken);
                foreach (var claim in jwtToken.Claims)
                {
                    if (claimTypeList.Contains(claim.Type))
                    {
                        customerClaims.Add(claim);
                    }
                }
            }

            return customerClaims;
        }

        public async Task SendToAll(SignalRData data)
        {
            var serviceHubContext = await serviceManagerStore.GetOrAddByConnectionString(connectionString).GetAsync(HubName);
            await serviceHubContext.Clients.All.SendCoreAsync(data.Target, data.Arguments);
        }

        public async Task SendToConnection(string connectionId, SignalRData data)
        {
            var serviceHubContext = await serviceManagerStore.GetOrAddByConnectionString(connectionString).GetAsync(HubName);
            await serviceHubContext.Clients.Client(connectionId).SendCoreAsync(data.Target, data.Arguments);
        }

        public async Task SendToUser(string userId, SignalRData data)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException($"{nameof(userId)} cannot be null or empty");
            }
            var serviceHubContext = await serviceManagerStore.GetOrAddByConnectionString(connectionString).GetAsync(HubName);
            await serviceHubContext.Clients.User(userId).SendCoreAsync(data.Target, data.Arguments);
        }

        public async Task SendToGroup(string groupName, SignalRData data)
        {
            if (string.IsNullOrEmpty(groupName))
            {
                throw new ArgumentException($"{nameof(groupName)} cannot be null or empty");
            }
            var serviceHubContext = await serviceManagerStore.GetOrAddByConnectionString(connectionString).GetAsync(HubName);
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
            var serviceHubContext = await serviceManagerStore.GetOrAddByConnectionString(connectionString).GetAsync(HubName);
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
            var serviceHubContext = await serviceManagerStore.GetOrAddByConnectionString(connectionString).GetAsync(HubName);
            await serviceHubContext.UserGroups.RemoveFromGroupAsync(userId, groupName);
        }

        public async Task RemoveUserFromAllGroups(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException($"{nameof(userId)} cannot be null or empty");
            }
            var serviceHubContext = await serviceManagerStore.GetOrAddByConnectionString(connectionString).GetAsync(HubName);
            await serviceHubContext.UserGroups.RemoveFromAllGroupsAsync(userId);
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
            var serviceHubContext = await serviceManagerStore.GetOrAddByConnectionString(connectionString).GetAsync(HubName);
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
            var serviceHubContext = await serviceManagerStore.GetOrAddByConnectionString(connectionString).GetAsync(HubName);
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