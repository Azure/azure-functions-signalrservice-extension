// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Azure.SignalR;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal class ServiceHubContextStore : IInternalServiceHubContextStore
    {
        private readonly ConcurrentDictionary<string, (Lazy<Task<IServiceHubContext>> lazy, IServiceHubContext value)> store = new ConcurrentDictionary<string, (Lazy<Task<IServiceHubContext>>, IServiceHubContext value)>(StringComparer.OrdinalIgnoreCase);
        private readonly ILoggerFactory loggerFactory;
        private readonly IOptionsMonitor<ServiceManagerOptions> monitor;

        public IServiceManager ServiceManager { get; }

        public AccessKey[] AccessKeys => new AccessKey[] { new ServiceEndpoint(monitor.CurrentValue.ConnectionString).AccessKey };

        public ServiceHubContextStore(IOptionsMonitor<ServiceManagerOptions> optionsMonitor, IServiceManager serviceManager, ILoggerFactory loggerFactory)
        {
            monitor = optionsMonitor;
            ServiceManager = serviceManager;
            this.loggerFactory = loggerFactory;
        }

        public ValueTask<IServiceHubContext> GetAsync(string hubName)
        {
            var pair = store.GetOrAdd(hubName, 
                (new Lazy<Task<IServiceHubContext>>(
                    () => ServiceManager.CreateHubContextAsync(hubName, loggerFactory)), default));
            return GetAsyncCore(hubName, pair);
        }

        private ValueTask<IServiceHubContext> GetAsyncCore(string hubName, (Lazy<Task<IServiceHubContext>> lazy, IServiceHubContext value) pair)
        {
            if (pair.lazy == null)
            {
                return new ValueTask<IServiceHubContext>(pair.value);
            }
            else
            {
                return new ValueTask<IServiceHubContext>(GetFromLazyAsync(hubName, pair));
            }
        }

        private async Task<IServiceHubContext> GetFromLazyAsync(string hubName, (Lazy<Task<IServiceHubContext>> lazy, IServiceHubContext value) pair)
        {
            try
            {
                var value = await pair.lazy.Value;
                store.TryUpdate(hubName, (null, value), pair);
                return value;
            }
            catch (Exception)
            {
                store.TryRemove(hubName, out _);
                throw;
            }
        }
    }
}