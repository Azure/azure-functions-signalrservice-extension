// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal static class Constants
    {
        public const string AzureSignalRConnectionStringName = "AzureSignalRConnectionString";
        public const string ServiceTransportTypeName = "AzureSignalRServiceTransportType";
        public const string SignalRTriggerHttpHeaderPrefix = "X-SignalR-Serverless";
        public static readonly string ConnectionIdHeader = $"{SignalRTriggerHttpHeaderPrefix}-ConnectionId";
        public static readonly string UserIdHeader = $"{SignalRTriggerHttpHeaderPrefix}-UserId";
        public const string JsonContentType = "application/json";
        public const string MessagepackContentType = "application/x-msgpack";
    }
}
