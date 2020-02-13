// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Executors;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal interface ISignalRTriggerRouter
    {
        void MapRouter((string hubName, string methodName) key, ITriggeredFunctionExecutor executor);

        Task<HttpResponseMessage> ProcessAsync(HttpRequestMessage req, CancellationToken token = default);
    }
}
