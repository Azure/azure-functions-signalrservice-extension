// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal class Utils
    {
        public static AzureSignalRClient GetAzureSignalRClient(string connectionStringKey, string attributeHubName)
        {
            return new AzureSignalRClient(StaticServiceHubContextStore.ServiceManagerStore, connectionStringKey, attributeHubName);
        }
    }
}