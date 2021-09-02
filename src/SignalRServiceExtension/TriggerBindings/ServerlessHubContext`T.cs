// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.Azure.SignalR.Management;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    //A helper class so that nameof(THub) together with T can be used as a key to retrieve a ServiceHubContext<T> from a ServiceProvider.
    internal class ServerlessHubContext<THub, T> where THub : ServerlessHub<T> where T : class
    {
        public Task<ServiceHubContext<T>> HubContextTask { get; }

        public ServerlessHubContext(ServiceManager serviceManager)
        {
            HubContextTask = serviceManager.CreateHubContextAsync<T>(typeof(THub).Name, default);
        }
    }
}
