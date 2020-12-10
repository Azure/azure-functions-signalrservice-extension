// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    /// <summary>
    /// A global <see cref="IServiceManagerStore"/> for the extension.
    /// It stores <see cref="IServiceHubContextStore"/> per connection string.
    /// </summary>
    public static class StaticServiceHubContextStore
    {
        /// <summary>
        /// Gets <see cref="IServiceHubContextStore"/>. 
        /// If the <see cref="IServiceHubContextStore"/> for a specific connection string exists, returns the <see cref="IServiceHubContextStore"/>,
        /// otherwise creates one and then returns it.
        /// </summary>
        /// <param name="configurationKey"> is the connection string configuration key.</param>
        /// <returns>The returned value is an instance of <see cref="IServiceHubContextStore"/>.</returns>
        public static IServiceHubContextStore Get(string configurationKey = Constants.AzureSignalRConnectionStringName) =>
            ServiceManagerStore.GetOrAddByConnectionStringKey(configurationKey);

        internal static IServiceManagerStore ServiceManagerStore { get; set; }
    }
}
