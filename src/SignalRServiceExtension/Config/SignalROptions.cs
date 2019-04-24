// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.SignalR.Management;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    public class SignalROptions
    {
        public string ConnectionString { get; set; }
        public string HubName { get; set; }
        public ServiceTransportType ServiceTransportType { get; set; } = ServiceTransportType.Persistent;
    }
}