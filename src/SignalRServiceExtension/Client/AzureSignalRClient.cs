// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
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
    internal class AzureSignalRClient : IAzureSignalRSender
    {
        private readonly IServiceHubContextStore serviceHubContextStore;
        private readonly IServiceManager serviceManager;

        internal AzureSignalRClient(IServiceHubContextStore serviceHubContextStore, IServiceManager serviceManager)
        {
            this.serviceHubContextStore = serviceHubContextStore;
            this.serviceManager = serviceManager;
        }

        internal SignalRConnectionInfo GetClientConnectionInfo(string hubName, string userId)
        {
            return new SignalRConnectionInfo
            {
                Url = serviceManager.GetClientEndpoint(hubName),
                AccessToken = serviceManager.GenerateClientAccessToken(hubName, userId)
            };
        }

        public async Task SendToAll(string hubName, SignalRData data)
        {
            var serviceHubContext = await serviceHubContextStore.GetOrAddAsync(hubName);
            await serviceHubContext.Clients.All.SendCoreAsync(data.Target, data.Arguments);
        }

        public async Task SendToUser(string hubName, string userId, SignalRData data)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException($"{nameof(userId)} cannot be null or empty");
            }
            var serviceHubContext = await serviceHubContextStore.GetOrAddAsync(hubName);
            await serviceHubContext.Clients.User(userId).SendCoreAsync(data.Target, data.Arguments);
        }

        public async Task SendToGroup(string hubName, string groupName, SignalRData data)
        {
            if (string.IsNullOrEmpty(groupName))
            {
                throw new ArgumentException($"{nameof(groupName)} cannot be null or empty");
            }
            var serviceHubContext = await serviceHubContextStore.GetOrAddAsync(hubName);
            await serviceHubContext.Clients.Group(groupName).SendCoreAsync(data.Target, data.Arguments);
        }

        public async Task AddUserToGroup(string hubName, string userId, string groupName)
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

        public async Task RemoveUserFromGroup(string hubName, string userId, string groupName)
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
    }
}