// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Azure.SignalR.Serverless.Protocols;
using Microsoft.Azure.WebJobs.Extensions.SignalRService.Exceptions;
using Microsoft.Azure.WebJobs.Host.Executors;
using InvocationMessage = Microsoft.Azure.SignalR.Serverless.Protocols.InvocationMessage;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal class SignalRHubMethodExecutor
    {
        private readonly Dictionary<string, ExecutionContext> _messageExecutors = new Dictionary<string, ExecutionContext>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, ExecutionContext> _connectionExecutors = new Dictionary<string, ExecutionContext>(StringComparer.OrdinalIgnoreCase);
        private readonly IRequestResolver _resolver;

        public string Hub { get; set; }

        public SignalRHubMethodExecutor(string hub, IRequestResolver resolver)
        {
            Hub = hub ?? throw new ArgumentNullException(nameof(hub));
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        public void Map((string category, string method) key, ExecutionContext executor)
        {
            if (key.category == Constants.Category.Connections)
            {
                _connectionExecutors.Add(key.method, executor);
            }
            else if (key.category == Constants.Category.Messages)
            {
                _messageExecutors.Add(key.method, executor);
            }
            else
            {
                throw new ArgumentException($"{key.category} is not a supported category");
            }
        }

        public async Task<HttpResponseMessage> ExecuteInvocation(HttpRequestMessage request)
        {
            if (!_resolver.TryGetInvocationContext(request, out var context))
            {
                //TODO: More detailed exception
                throw new SignalRTriggerException();
            }
            var (message, protocol) = await GetMessageAsync<InvocationMessage>(request);
            AssertConsistency(context, message);
            context.Arguments = message.Arguments;

            // Only when it's an invoke, we need the result from function execution.
            TaskCompletionSource<object> tcs = null;
            if (!string.IsNullOrEmpty(message.InvocationId))
            {
                tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            HttpResponseMessage response;
            CompletionMessage completionMessage = null;
            if (_messageExecutors.TryGetValue(context.Event, out var executor))
            {
                var functionResult = await ExecuteWithAuthAsync(request, executor, context, tcs);
                if (tcs != null)
                {
                    if (!functionResult.Succeeded)
                    {
                        // TODO: Consider more error details
                        completionMessage = CompletionMessage.WithError(message.InvocationId, "Execution failed");
                        response = new HttpResponseMessage(HttpStatusCode.OK);
                    }
                    else
                    {
                        var result = await tcs.Task;
                        completionMessage = CompletionMessage.WithResult(message.InvocationId, result);
                        response = new HttpResponseMessage(HttpStatusCode.OK);
                    }
                }
                else
                {
                    response = new HttpResponseMessage(HttpStatusCode.OK);
                }
            }
            else
            {
                response = new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            if (completionMessage != null)
            {
                response.Content = new ByteArrayContent(protocol.GetMessageBytes(completionMessage).ToArray());
            }
            return response;
        }

        public async Task<HttpResponseMessage> ExecuteOpenConnection(HttpRequestMessage request)
        {
            if (!_resolver.TryGetInvocationContext(request, out var context))
            {
                //TODO: More detailed exception
                throw new SignalRTriggerException();
            }

            if (_connectionExecutors.TryGetValue(Constants.Events.Connect, out var executor))
            {
                var result = await ExecuteWithAuthAsync(request, executor, context);
                if (!result.Succeeded)
                {
                    return new HttpResponseMessage(HttpStatusCode.Forbidden);
                }
            }
            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        public async Task<HttpResponseMessage> ExecuteCloseConnection(HttpRequestMessage request)
        {
            if (!_resolver.TryGetInvocationContext(request, out var context))
            {
                //TODO: More detailed exception
                throw new SignalRTriggerException();
            }
            var (message, _) = await GetMessageAsync<CloseConnectionMessage>(request);
            context.Error = message.Error;

            if (_connectionExecutors.TryGetValue(Constants.Events.Disconnect, out var executor))
            {
                await ExecuteWithAuthAsync(request, executor, context);
            }
            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        private Task<FunctionResult> ExecuteWithAuthAsync(HttpRequestMessage request, ExecutionContext executor,
            InvocationContext context, TaskCompletionSource<object> tcs = null)
        {
            if (!_resolver.ValidateSignature(request, executor.AccessKey))
            {
                //TODO: More detailed exception
                throw new SignalRTriggerException();
            }

            return ExecuteAsync(executor.Executor, context);
        }

        private async Task<FunctionResult> ExecuteAsync(ITriggeredFunctionExecutor executor, InvocationContext context, TaskCompletionSource<object> tcs = null)
        {
            var signalRTriggerEvent = new SignalRTriggerEvent
            {
                Context = context,
                TaskCompletionSource = tcs,
            };

            var result = await executor.TryExecuteAsync(
                new TriggeredFunctionData
                {
                    TriggerValue = signalRTriggerEvent
                }, CancellationToken.None);

            // If there's exception in invocation, tcs may not be set.
            // And SetException seems not necessary. Exception can be get from FunctionResult.
            if (result.Succeeded == false)
            {
                tcs?.TrySetResult(null);
            }

            return result;
        }

        private async Task<(T, IHubProtocol)> GetMessageAsync<T>(HttpRequestMessage request) where T: ServerlessMessage
        {
            var payload = new ReadOnlySequence<byte>(await request.Content.ReadAsByteArrayAsync());
            var messageParser = MessageParser.GetParser(request.Content.Headers.ContentType.MediaType);
            if (!messageParser.TryParseMessage(ref payload, out var message))
            {
                throw new SignalRTriggerException("Parsing message failed");
            }

            return ((T)message, messageParser.Protocol);
        }

        private void AssertConsistency(InvocationContext context, InvocationMessage message)
        {
            if (!string.Equals(context.Event, message.Target, StringComparison.OrdinalIgnoreCase))
            {
                // TODO: More detailed exception
                throw new SignalRTriggerException();
            }
        }
    }
}
