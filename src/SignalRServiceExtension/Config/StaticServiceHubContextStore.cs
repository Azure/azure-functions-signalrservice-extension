// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.Azure.SignalR.Management;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    /// <summary>
    /// A global <see cref="IServiceHubContextStore"/> for the extension.
    /// It stores <see cref="IServiceHubContext"/> per hub.
    /// </summary>
    public static class StaticServiceHubContextStore
    {
        /// <summary>
        /// Gets or adds <see cref="IServiceHubContext"/>. 
        /// If the <see cref="IServiceHubContext"/> for a specific hub exists, returns the <see cref="IServiceHubContext"/>,
        /// otherwise creates one and then returns it.
        /// </summary>
        /// <param name="hubName">Hub name of the <see cref="IServiceHubContext"/></param>
        /// <returns><see cref="IServiceHubContext"/> which is a context abstraction for a hub.</returns>
        public static ValueTask<IServiceHubContext> GetOrAddAsync(string hubName) =>
            ServiceHubContextStore.GetOrAddAsync(hubName);

        internal static IServiceHubContextStore ServiceHubContextStore { get; set; }
    }
}
