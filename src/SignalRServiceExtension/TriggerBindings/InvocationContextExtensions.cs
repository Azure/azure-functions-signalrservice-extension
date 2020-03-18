// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.SignalR.Management;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    public static class InvocationContextExtensions
    {
        /// <summary>
        /// Gets an object that can be used to invoke methods on the clients connected to this hub.
        /// </summary>
        public static async Task<IHubClients> GetClientsAsync(this InvocationContext invocationContext)
        {
            return (await StaticServiceHubContextStore.Get().GetAsync(invocationContext.Hub)).Clients;
        }

        /// <summary>
        /// Get the group manager of this hub.
        /// </summary>
        public static async Task<IGroupManager> GetGroupsAsync(this InvocationContext invocationContext)
        {
            return (await StaticServiceHubContextStore.Get().GetAsync(invocationContext.Hub)).Groups;
        }

        /// <summary>
        /// Get the user group manager of this hub.
        /// </summary>
        public static async Task<IUserGroupManager> GetUserGroupManagerAsync(this InvocationContext invocationContext)
        {
            return (await StaticServiceHubContextStore.Get().GetAsync(invocationContext.Hub)).UserGroups;
        }
    }
}
