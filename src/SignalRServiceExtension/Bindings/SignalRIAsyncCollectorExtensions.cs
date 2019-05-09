// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;

namespace Microsoft.Azure.WebJobs
{
    public static class SignalRIAsyncCollectorExtensions
    {
        public static ValueTask<IServiceHubContext> GetServiceHubContextAsync(this IAsyncCollector<SignalRMessage> collector, string hubName) =>
            StaticServiceHubContextStore.ServiceHubContextStore.GetOrAddAsync(hubName);
    }
}