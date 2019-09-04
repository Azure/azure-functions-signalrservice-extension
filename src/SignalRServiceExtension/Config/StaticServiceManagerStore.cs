// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.SignalR.Management;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    /// <summary>
    /// A global <see cref="IServiceManagerStore"/> for the extension.
    /// It stores <see cref="IServiceManagerStore"/> per connection string.
    /// </summary>
    public static class StaticServiceManagerStore
    {
        /// <summary>
        /// Gets or adds <see cref="IServiceHubContextStore"/>. 
        /// If the <see cref="IServiceHubContextStore"/> for a specific connection string exists, returns the <see cref="IServiceHubContextStore"/>,
        /// otherwise creates one and then returns it.
        /// </summary>
        /// <param name="connectionString"> is the connection string of the <see cref="IServiceManager"/></param>
        /// <returns>The returned value is an instance of <see cref="IServiceHubContextStore"/>.</returns>
        public static IServiceHubContextStore GetOrAdd(string configurationKey = null) =>
            ServiceManagerStore.GetOrAddByConfigurationKey(string.IsNullOrEmpty(configurationKey) ? Constants.AzureSignalRConnectionStringName : configurationKey);

        internal static IServiceManagerStore ServiceManagerStore { get; set; }
    }
}
