// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal class ServiceManagerStore : IServiceManagerStore
    {
        private readonly ILoggerFactory loggerFactory;
        private readonly ServiceTransportType transportType;
        private readonly IConfiguration configuration;
        private readonly ConcurrentDictionary<string, IServiceHubContextStore> store = new ConcurrentDictionary<string, IServiceHubContextStore>();

        public ServiceManagerStore(ServiceTransportType transportType, IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            this.loggerFactory = loggerFactory;
            this.transportType = transportType;
            this.configuration = configuration;
        }

        public IServiceHubContextStore GetOrAddByConfigurationKey(string configurationKey)
        {
            string connectionString = configuration[configurationKey];
            return GetOrAddByConnectionString(connectionString);
        }

        public IServiceHubContextStore GetOrAddByConnectionString(string connectionString)
        {
            return store.GetOrAdd(connectionString, CreateHubContextStore);
        }

        // test only
        public IServiceHubContextStore GetByConfigurationKey(string configurationKey)
        {
            string connectionString = configuration[configurationKey];
            return store.ContainsKey(connectionString) ? store[connectionString] : null;
        }

        private IServiceHubContextStore CreateHubContextStore(string connectionString)
        {
            var serviceManager = CreateServiceManager(connectionString);
            return new ServiceHubContextStore(serviceManager, loggerFactory);
        }

        private IServiceManager CreateServiceManager(string connectionString)
        {
            return new ServiceManagerBuilder().WithOptions(o =>
            {
                o.ConnectionString = connectionString;
                o.ServiceTransportType = transportType;
            }).WithCallingAssembly().Build();
        }
    }
}
