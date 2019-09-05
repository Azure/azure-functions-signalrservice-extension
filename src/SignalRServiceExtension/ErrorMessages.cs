// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal static class ErrorMessages
    {
        public static readonly string EmptyConnectionStringErrorMessageFormat =
            $"The SignalR Service connection string must be set either via an '{Constants.AzureSignalRConnectionStringName}' app setting, via an '{Constants.AzureSignalRConnectionStringName}' environment variable, or directly in code via {nameof(SignalROptions)}.{nameof(SignalROptions.ConnectionString)} or {{0}}.";
    }
}
