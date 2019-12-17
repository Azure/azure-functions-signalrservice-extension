using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal class SignalRHubMethodExecutor
    {
        private const string OnConnectedTarget = "OnConnected";
        private const string OnDisconnectedTarget = "OnDisconnected";

        private readonly Dictionary<string, ITriggeredFunctionExecutor> _executors = new Dictionary<string, ITriggeredFunctionExecutor>(StringComparer.OrdinalIgnoreCase);

        public string Hub { get; set; }

        public SignalRHubMethodExecutor(string hub)
        {
            Hub = hub ?? throw new ArgumentNullException(nameof(hub));
        }

        public void AddTarget(string target, ITriggeredFunctionExecutor executor)
        {
            _executors.Add(target, executor);
        }

        public async Task<HttpResponseMessage> ExecuteInvocation(IHubProtocol protocol, InvocationContext.ConnectionContext context, InvocationMessage message)
        {
            string target = message.Target;
            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            HttpResponseMessage response;
            CompletionMessage completionMessage;
            if (_executors.TryGetValue(target, out var executor))
            {
                await ExecuteAsync(executor, context, message, tcs);
                var result = await tcs.Task;
                completionMessage = CompletionMessage.WithResult(message.InvocationId, result);
                response = new HttpResponseMessage(HttpStatusCode.OK);
            }
            else
            {
                completionMessage = CompletionMessage.WithError(message.InvocationId, $"Target: {target} not found.");
                response = new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            if (!string.IsNullOrEmpty(message.InvocationId))
            {
                response.Content = new ByteArrayContent(protocol.GetMessageBytes(completionMessage).ToArray());
            }
            return response;
        }

        public async Task<HttpResponseMessage> ExecuteOpenConnection(InvocationContext.ConnectionContext context, OpenConnectionMessage message)
        {
            if (_executors.TryGetValue(OnConnectedTarget, out var executor))
            {
                await ExecuteAsync(executor, context, message, null);
            }
            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        public async Task<HttpResponseMessage> ExecuteCloseConnection(InvocationContext.ConnectionContext context, CloseConnectionMessage message)
        {
            if (_executors.TryGetValue(OnDisconnectedTarget, out var executor))
            {
                await ExecuteAsync(executor, context, message, null);
            }
            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        private async Task ExecuteAsync(ITriggeredFunctionExecutor executor, InvocationContext.ConnectionContext context, ISignalRServerlessMessage message, TaskCompletionSource<object> tcs)
        {
            var signalRTriggerEvent = new SignalRTriggerEvent
            {
                Context = new InvocationContext
                {
                    Context = context,
                    Data = message,
                },
                TaskCompletionSource = tcs,
            };

            await executor.TryExecuteAsync(
                new TriggeredFunctionData
                {
                    TriggerValue = signalRTriggerEvent
                }, CancellationToken.None);
        }


    }
}
