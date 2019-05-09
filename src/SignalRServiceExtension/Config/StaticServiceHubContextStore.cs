// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    public static class StaticServiceHubContextStore
    {
        public static IServiceHubContextStore ServiceHubContextStore { get; internal set; }
    }
}
