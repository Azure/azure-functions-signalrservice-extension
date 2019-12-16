// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal static class Constants
    {
        public const string AzureSignalRConnectionStringName = "AzureSignalRConnectionString";
        public const string ServiceTransportTypeName = "AzureSignalRServiceTransportType";
        public const string AsrsHeaderPrefix = "X-ASRS-";
        public const string AsrsConnectionIdHeader = AsrsHeaderPrefix + "ConnectionId";
        public const string AsrsUserIdHeader = AsrsHeaderPrefix + "UserId";
        public const string AsrsHubNameHeader = AsrsHeaderPrefix + "HubName";
        public const string JsonContentType = "application/json";
        public const string MessagePackContentType = "application/x-msgpack";
    }
}
