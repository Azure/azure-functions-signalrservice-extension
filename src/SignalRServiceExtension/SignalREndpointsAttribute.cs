// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Azure.WebJobs.Description;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    [AttributeUsage(AttributeTargets.Parameter)]
    [Binding]
    public class SignalREndpointsAttribute : Attribute
    {
        public string ConnectionStringSetting { get; set; } = Constants.AzureSignalRConnectionStringName;

        //todo resolve hub name from SignalRAttribute
        [AutoResolve]
        public string HubName { get; set; }
    }
}