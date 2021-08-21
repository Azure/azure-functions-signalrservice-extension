// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.SignalR.Management;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    /// <summary>
    /// Extends the functionality of <see cref="ServerlessHub"/> with a strongly typed client proxy
    /// </summary>
    /// <typeparam name="T">The type of client.</typeparam>
    public abstract class ServerlessHub<T> : ServerlessHub
    {
        protected ServerlessHub(IServiceHubContext hubContext = null, IServiceManager serviceManager = null)
          : base(hubContext, serviceManager)
        {
            Clients = new TypedHubClients<T>(base.Clients);
        }

        public new IHubClients<T> Clients { get; }
    }
}
