// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Azure.WebJobs.Host.Executors;

namespace SignalRServiceExtension.Tests.Utils
{
    class TestTriggerDispatcher : ISignalRTriggerDispatcher
    {
        public Dictionary<(string, string, string), ITriggeredFunctionExecutor> Executors { get; } =
            new Dictionary<(string, string, string), ITriggeredFunctionExecutor>();

        public void Map((string hubName, string category, string @event) key, ITriggeredFunctionExecutor executor)
        {
            Executors.Add(key, executor);
        }

        public Task<HttpResponseMessage> ExecuteAsync(HttpRequestMessage req, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }
    }
}
