// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.SignalR;
using Microsoft.Azure.SignalR.Management;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal class ServiceHubContextStore : IInternalServiceHubContextStore
    {
        private readonly ConcurrentDictionary<string, Lazy<Task<IServiceHubContext>>> store = new(StringComparer.OrdinalIgnoreCase);
        private readonly IServiceEndpointManager endpointManager;

        public IServiceManager ServiceManager { get; }

        public AccessKey[] AccessKeys => endpointManager.Endpoints.Keys.Select(endpoint => endpoint.AccessKey).ToArray();

        public ServiceHubContextStore(IServiceEndpointManager endpointManager, IServiceManager serviceManager)
        {
            this.endpointManager = endpointManager;
            ServiceManager = serviceManager;
        }

        public ValueTask<IServiceHubContext> GetAsync(string hubName)
        {
            var pair = store.GetOrAdd(hubName,
                new Lazy<Task<IServiceHubContext>>(
                    () => ServiceManager.CreateHubContextAsync(hubName), true));
            return new ValueTask<IServiceHubContext>(pair.Value);
        }
    }
}