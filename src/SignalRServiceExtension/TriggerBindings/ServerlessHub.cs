﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.SignalR.Management;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    /// <summary>
    /// When a class derived from <see cref="ServerlessHub"/>,
    /// all the methods in the class are identified as using class based model.
    /// <b>HubName</b> is resolved from class name.
    /// <b>Event</b> is resolved from method name.
    /// <b>Category</b> is determined by the method name. Only <b>OnConnected</b> and <b>OnDisconnected</b> will
    /// be considered as Connections and others will be Messages.
    /// <b>ParameterNames</b> will be automatically resolved by all the parameters of the method in order, except the
    /// parameter which belongs to a binding parameter, or has the type of <see cref="Microsoft.Extensions.Logging.ILogger"/> or
    /// <see cref="System.Threading.CancellationToken"/>, or marked by <see cref="SignalRIgnoreAttribute"/>.
    /// Note that <see cref="SignalRTriggerAttribute"/> MUST use parameterless constructor in class based model.
    /// </summary>
    public abstract class ServerlessHub : IDisposable
    {
        private static readonly Lazy<JwtSecurityTokenHandler> JwtSecurityTokenHandler = new Lazy<JwtSecurityTokenHandler>(() => new JwtSecurityTokenHandler());
        private bool _disposed;
        private readonly IServiceManager _serviceManager;

        public ServerlessHub()
        {
            HubName = GetType().Name;
            var store = StaticServiceHubContextStore.Get();
            var hubContext = store.GetAsync(HubName).GetAwaiter().GetResult();
            _serviceManager = store.ServiceManager;
            Clients = hubContext.Clients;
            Groups = hubContext.Groups;
            UserGroups = hubContext.UserGroups;
        }

        /// <summary>
        /// Gets an object that can be used to invoke methods on the clients connected to this hub.
        /// </summary>
        public IHubClients Clients { get; }

        /// <summary>
        /// Get the group manager of this hub.
        /// </summary>
        public IGroupManager Groups { get; }

        /// <summary>
        /// Get the user group manager of this hub.
        /// </summary>
        public IUserGroupManager UserGroups { get; }

        /// <summary>
        /// Get the hub name of this hub.
        /// </summary>
        public string HubName { get; }

        /// <summary>
        /// Return a <see cref="SignalRConnectionInfo"/> to finish a client negotiation.
        /// </summary>
        protected SignalRConnectionInfo Negotiate(string userId = null, IList<Claim> claims = null, TimeSpan? lifeTime = null)
        {
            return new SignalRConnectionInfo
            {
                Url = _serviceManager.GetClientEndpoint(HubName),
                AccessToken = _serviceManager.GenerateClientAccessToken(HubName, userId, claims, lifeTime)
            };
        }

        /// <summary>
        /// Get claim list from a JWT.
        /// </summary>
        protected IList<Claim> GetClaims(string jwt)
        {
            if (jwt.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                jwt = jwt.Substring("Bearer ".Length).Trim();
            }
            return JwtSecurityTokenHandler.Value.ReadJwtToken(jwt).Claims.ToList();
        }

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
