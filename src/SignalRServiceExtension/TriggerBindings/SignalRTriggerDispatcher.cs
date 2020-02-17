// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.SignalR.Serverless.Protocols;
using Microsoft.Azure.WebJobs.Extensions.SignalRService.Exceptions;
using Microsoft.Azure.WebJobs.Host.Executors;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal class SignalRTriggerDispatcher : ISignalRTriggerDispatcher
    {
        private readonly Dictionary<string, SignalRHubMethodExecutor> _executors =
            new Dictionary<string, SignalRHubMethodExecutor>(StringComparer.OrdinalIgnoreCase);

        public void Map((string hubName, string methodName) key, ITriggeredFunctionExecutor executor)
        {
            if (!_executors.TryGetValue(key.hubName, out var hubExecutor))
            {
                hubExecutor = new SignalRHubMethodExecutor(key.hubName);
                _executors.Add(key.hubName, hubExecutor);
            }

            hubExecutor.AddTarget(key.methodName, executor);
        }

        public async Task<HttpResponseMessage> ExecuteAsync(HttpRequestMessage req, CancellationToken token = default)
        {
            // TODO: More details about response
            var contentType = req.Content.Headers.ContentType.MediaType;
            if (!ValidateContentType(contentType))
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }

            if (!ValidateSignature(req))
            {
                return new HttpResponseMessage(HttpStatusCode.Forbidden);
            }

            if (!InvocationContextHelper.TryGetInvocationContext(req, out var connectionContext))
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }

            var hubName = connectionContext.Hub;
            // TODO: select out executor that match the pattern
            if (_executors.TryGetValue(hubName, out var executor))
            {
                try
                {
                    var payload = new ReadOnlySequence<byte>(await req.Content.ReadAsByteArrayAsync());
                    var messageParser = MessageParser.GetParser(contentType);
                    if (!messageParser.TryParseMessage(ref payload, out var message))
                    {
                        throw new FailedRouteEventException("Parsing message failed");
                    }

                    if (connectionContext.Category == Constants.Category.Connections)
                    {
                        if (connectionContext.Event == Constants.Events.Connect)
                        {
                            AssertTypeMatch<OpenConnectionMessage>(message);
                            return await executor.ExecuteOpenConnection(connectionContext);
                        }

                        if (connectionContext.Event == Constants.Events.Disconnect)
                        {
                            connectionContext.Error = AssertTypeMatch<CloseConnectionMessage>(message).Error;
                            return await executor.ExecuteCloseConnection(connectionContext);
                        }

                        throw new FailedRouteEventException($"{connectionContext.Event} is not a supported event");
                    }

                    if (connectionContext.Category == Constants.Category.Messages)
                    {
                        var invocationMessage = AssertTypeMatch<InvocationMessage>(message);
                        connectionContext.Arguments = invocationMessage.Arguments;
                        return await executor.ExecuteInvocation(messageParser.Protocol, connectionContext, invocationMessage.Target, invocationMessage.InvocationId);
                    }

                    throw new FailedRouteEventException($"{connectionContext.Category} is not a supported category");
                }
                catch (SignalRBindingException ex)
                {
                    return new HttpResponseMessage(HttpStatusCode.BadRequest)
                    {
                        ReasonPhrase = ex.Message
                    };
                }
            }

            // No target hub in functions
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        private bool ValidateContentType(string contentType)
        {
            if (string.IsNullOrEmpty(contentType))
            {
                return false;
            }
            return contentType == Constants.JsonContentType || contentType == Constants.MessagePackContentType;
        }

        private bool ValidateSignature(HttpRequestMessage request)
        {
            if (!request.Headers.Contains(Constants.AsrsSignature))
            {
                return false;
            }

            //TODO: Add real signature validation
            return true;
        }

        private T AssertTypeMatch<T>(object obj)
        {
            if (obj.GetType() != typeof(T))
            {
                throw new FailedRouteEventException($"Message type: {obj.GetType()} doesn't match the expected type: {typeof(T)}");
            }

            return (T)obj;
        }
    }
}
