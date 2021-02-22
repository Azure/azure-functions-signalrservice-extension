// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Azure.SignalR;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

#if NETCOREAPP3_1
using Microsoft.AspNetCore.SignalR.Protocol;
#endif

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal class ServiceManagerStore : IServiceManagerStore
    {
        private readonly ILoggerFactory loggerFactory;
        private readonly IConfiguration configuration;
        private readonly IEndpointRouter router;
        private readonly ConcurrentDictionary<string, IInternalServiceHubContextStore> store = new ConcurrentDictionary<string, IInternalServiceHubContextStore>();

        public ServiceManagerStore(IConfiguration configuration, ILoggerFactory loggerFactory, IEndpointRouter router = null)
        {
            this.loggerFactory = loggerFactory;
            this.configuration = configuration;
            this.router = router;
        }

        public IInternalServiceHubContextStore GetOrAddByConnectionStringKey(string connectionStringKey)
        {
            if (string.IsNullOrWhiteSpace(connectionStringKey))
            {
                throw new ArgumentException($"'{nameof(connectionStringKey)}' cannot be null or whitespace", nameof(connectionStringKey));
            }
            return store.GetOrAdd(connectionStringKey, CreateHubContextStore);
        }

        //test only
        public IInternalServiceHubContextStore GetByConfigurationKey(string connectionStringKey)
        {
            return store.ContainsKey(connectionStringKey) ? store[connectionStringKey] : null;
        }

        private IInternalServiceHubContextStore CreateHubContextStore(string connectionStringKey)
        {
            var services = new ServiceCollection()
                .WithAssembly(Assembly.GetExecutingAssembly())
                .SetupOptions<ServiceManagerOptions, OptionsSetup>(new OptionsSetup(configuration, loggerFactory, connectionStringKey))
                .PostConfigure<ServiceManagerOptions>(o =>
                {
                    if ((o.ServiceEndpoints == null || o.ServiceEndpoints.Length == 0) && string.IsNullOrWhiteSpace(o.ConnectionString))
                    {
                        throw new InvalidOperationException(ErrorMessages.EmptyConnectionStringErrorMessageFormat);
                    }
                })
                .AddSignalRServiceManager()
                .AddSingleton(loggerFactory)
                .AddSingleton<IInternalServiceHubContextStore, ServiceHubContextStore>();
            if (router != null)
            {
                services.AddSingleton(router);
            }
#if NETCOREAPP3_1
            var jsonProtocols = services.Where(s => s.ServiceType == typeof(IHubProtocol) && s.ImplementationType == typeof(JsonHubProtocol)).ToArray();
            foreach (var protocol in jsonProtocols)
            {
                services.Remove(protocol);
            }
            services.AddSingleton<IHubProtocol, NewtonsoftJsonHubProtocol>();
#endif
            services.AddSingleton(services.ToList() as IReadOnlyCollection<ServiceDescriptor>);
            return services.BuildServiceProvider()
               .GetRequiredService<IInternalServiceHubContextStore>();
        }
    }
}