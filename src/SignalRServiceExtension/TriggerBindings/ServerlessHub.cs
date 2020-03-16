// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.SignalR.Management;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    /// <summary>
    /// When a class derived from <see cref="ServerlessHub"/>,
    /// all the method in the are identified as using class based model.
    /// </summary>
    public abstract class ServerlessHub : IDisposable
    {
        private bool _disposed;

        public ServerlessHub()
        {
            var hubName = GetType().Name;
            var hubContext = StaticServiceHubContextStore.Get().GetAsync(hubName).GetAwaiter().GetResult();
            Clients = hubContext.Clients;
            Groups = hubContext.Groups;
            UserGroups = hubContext.UserGroups;
        }

        public IHubClients Clients { get; }

        public IGroupManager Groups { get; }

        public IUserGroupManager UserGroups { get; }

        /// <summary>
        /// Releases all resources currently used by this <see cref="ServerlessHub" /> instance.
        /// </summary>
        /// <param name="disposing"><c>true</c> if this method is being invoked by the <see cref="M:Microsoft.Azure.WebJobs.Extensions.SignalRService.Serverless.Dispose" /> method,
        /// otherwise <c>false</c>.</param>
        protected virtual void Dispose(bool disposing)
        {
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
                
            Dispose(true);
            _disposed = true;
        }
    }
}
