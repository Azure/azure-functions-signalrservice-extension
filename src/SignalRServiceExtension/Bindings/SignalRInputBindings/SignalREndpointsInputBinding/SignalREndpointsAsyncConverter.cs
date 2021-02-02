﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.SignalR.Management;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal class SignalREndpointsAsyncConverter : IAsyncConverter<SignalREndpointsAttribute, LiteServiceEndpoint[]>
    {
        private readonly IServiceManagerStore _serviceManagerStore;

        public SignalREndpointsAsyncConverter()
        {
            _serviceManagerStore = StaticServiceHubContextStore.ServiceManagerStore;
        }

        public async Task<LiteServiceEndpoint[]> ConvertAsync(SignalREndpointsAttribute input, CancellationToken cancellationToken)
        {
            var hubContext = await _serviceManagerStore.GetOrAddByConnectionStringKey(input.ConnectionStringSetting).ServiceManager.CreateHubContextAsync(input.HubName) as IInternalServiceHubContext;
            return hubContext.GetServiceEndpoints().Select(e => LiteServiceEndpoint.FromServiceEndpoint(e)).ToArray();
        }
    }
}