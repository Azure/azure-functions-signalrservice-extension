// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal class Utils
    {
        public static string FirstOrDefault(params string[] values)
        {
            return values.FirstOrDefault(v => !string.IsNullOrEmpty(v));
        }

        public static AzureSignalRClient GetAzureSignalRClient(string connectionStringKey, string attributeHubName, SignalROptions options = null)
        {
            var hubName = FirstOrDefault(attributeHubName, options?.HubName);

            return new AzureSignalRClient(StaticServiceHubContextStore.ServiceManagerStore, connectionStringKey, hubName);
        }
    }
}
