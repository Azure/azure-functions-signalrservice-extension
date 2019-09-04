// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal interface IServiceManagerStore
    {
        IServiceHubContextStore GetOrAddByConfigurationKey(string configurationKey);

        IServiceHubContextStore GetOrAddByConnectionString(string connectionString);
    }
}
