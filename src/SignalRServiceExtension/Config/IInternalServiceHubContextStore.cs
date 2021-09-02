// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.SignalR;
using Microsoft.Azure.SignalR.Management;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal interface IInternalServiceHubContextStore : IServiceHubContextStore
    {
        AccessKey[] AccessKeys { get; }

        public dynamic GetAsync(Type THubType, Type TType);
    }
}