// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal static class DependencyInjectionExtensions
    {
        private const string HubProtocolError = "It's invalid to configure hub protocol on Azure Functions runtime V2. Newtonsoft.Json protocol will be used.";

        public static IServiceCollection SetHubProtocol(this IServiceCollection services, IConfiguration configuration)
        {
            if (Environment.Version.Major == 4 && configuration[Constants.AzureSignalRHubProtocol] != null)
            {
                // Actually is .Net Core 2.x
                throw new InvalidOperationException(HubProtocolError);
            }
#if NETCOREAPP3_1 || NETCOREAPP3_0 || NETSTANDARD2_0 
            else if (!DotnetRuntime(configuration) || UserSpecifyNewtonsoft(configuration))
            {
                // .Net Core 3.1, overwrite the System.Text.Json Protocol.
                services.TryAddEnumerable(ServiceDescriptor.Singleton<IHubProtocol, NewtonsoftJsonHubProtocol>());
            }
#endif
            return services;
        }

        private static bool DotnetRuntime(IConfiguration configuration)
        {
            var workerRuntime = configuration[Constants.FunctionsWorkerRuntime];
            //unit test environment
            return workerRuntime == null || workerRuntime == Constants.DotnetWorker;
        }

        private static bool UserSpecifyNewtonsoft(IConfiguration configuration)
        {
            return configuration.GetValue(Constants.AzureSignalRHubProtocol, HubProtocol.SystemTextJson) == HubProtocol.NewtonsoftJson;
        }
    }
}