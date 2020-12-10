// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Azure.SignalR;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal class ServiceManagerStore : IServiceManagerStore
    {
        private readonly ILoggerFactory loggerFactory;
        private readonly IConfiguration configuration;
        private readonly ConcurrentDictionary<string, IServiceHubContextStore> store = new ConcurrentDictionary<string, IServiceHubContextStore>();

        public ServiceManagerStore(IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            this.loggerFactory = loggerFactory;
            this.configuration = configuration;
        }

        public IServiceHubContextStore GetOrAddByConnectionStringKey(string connectionStringKey)
        {
            if (string.IsNullOrWhiteSpace(connectionStringKey))
            {
                throw new ArgumentException($"'{nameof(connectionStringKey)}' cannot be null or whitespace", nameof(connectionStringKey));
            }
            return store.GetOrAdd(connectionStringKey, CreateHubContextStore);
        }

        //test only
        public IServiceHubContextStore GetByConfigurationKey(string connectionStringKey)
        {
            return store.ContainsKey(connectionStringKey) ? store[connectionStringKey] : null;
        }

        private IServiceHubContextStore CreateHubContextStore(string connectionStringKey)
        {
            return new ServiceCollection().AddSignalRServiceManager()
                .WithAssembly(Assembly.GetExecutingAssembly())
                .SetupOptions<ServiceManagerOptions, OptionsSetup>(new OptionsSetup(configuration, loggerFactory, connectionStringKey))
                .PostConfigure<ServiceManagerOptions>(o =>
                {
                    if (string.IsNullOrWhiteSpace(o.ConnectionString))
                    {
                        throw new InvalidOperationException(ErrorMessages.EmptyConnectionStringErrorMessageFormat);
                    }
                })
                .AddSingleton(loggerFactory)
                .AddSingleton(configuration)
                .AddSingleton<IServiceHubContextStore, ServiceHubContextStore>()
                .BuildServiceProvider()
                .GetRequiredService<IServiceHubContextStore>();
        }
    }
}