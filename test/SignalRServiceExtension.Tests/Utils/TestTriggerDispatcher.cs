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
        public Dictionary<(string, string), ITriggeredFunctionExecutor> Executors { get; } =
            new Dictionary<(string, string), ITriggeredFunctionExecutor>();

        public void Map((string hubName, string methodName) key, ITriggeredFunctionExecutor executor)
        {
            Executors.Add(key, executor);
        }

        public Task<HttpResponseMessage> ExecuteAsync(HttpRequestMessage req, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }
    }
}
