// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.Azure.SignalR.Management;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal interface IServiceHubContextStore
    {
        ValueTask<IServiceHubContext> GetOrAddAsync(string hubName);
    }
}
