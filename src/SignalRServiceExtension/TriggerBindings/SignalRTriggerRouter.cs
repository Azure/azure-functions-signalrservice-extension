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

using Microsoft.Azure.WebJobs.Extensions.SignalRService.Exceptions;
using Microsoft.Azure.WebJobs.Host.Executors;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    public class SignalRTriggerRouter
    {
        private readonly Dictionary<string, SignalRHubMethodExecutor> _executors =
            new Dictionary<string, SignalRHubMethodExecutor>(StringComparer.OrdinalIgnoreCase);

        internal void AddRoute((string hubName, string methodName) key, ITriggeredFunctionExecutor executor)
        {
            if (!_executors.TryGetValue(key.hubName, out var hubExecutor))
            {
                hubExecutor = new SignalRHubMethodExecutor(key.hubName);
                _executors.Add(key.hubName, hubExecutor);
            }

            hubExecutor.AddTarget(key.methodName, executor);
        }

        public async Task<HttpResponseMessage> ProcessAsync(HttpRequestMessage req, CancellationToken token = default)
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

            if (!TryGetInvocationContext(req, out var connectionContext))
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
                            AssertTypeMatch(message.Type, ServerlessProtocolConstants.OpenConnectionMessageType);
                            return await executor.ExecuteOpenConnection(connectionContext);
                        }

                        if (connectionContext.Event == Constants.Events.Disconnect)
                        {
                            AssertTypeMatch(message.Type, ServerlessProtocolConstants.CloseConnectionMessageType);
                            connectionContext.Error = ((CloseConnectionMessage)message).Error;
                            return await executor.ExecuteCloseConnection(connectionContext);
                        }

                        throw new FailedRouteEventException($"{connectionContext.Event} is not a supported event");
                    }

                    if (connectionContext.Category == Constants.Category.Messages)
                    {
                        AssertTypeMatch(message.Type, ServerlessProtocolConstants.InvocationMessageType);
                        var invocationMessage = (InvocationMessage)message;
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

        private bool TryGetInvocationContext(HttpRequestMessage request, out InvocationContext context)
        {
            if (!request.Headers.Contains(Constants.AsrsHubNameHeader) ||
                !request.Headers.Contains(Constants.AsrsCategory) ||
                !request.Headers.Contains(Constants.AsrsEvent) ||
                !request.Headers.Contains(Constants.AsrsConnectionIdHeader))
            {
                context = null;
                return false;
            }

            context = new InvocationContext();
            context.ConnectionId = request.Headers.GetValues(Constants.AsrsConnectionIdHeader).First();
            context.Hub = request.Headers.GetValues(Constants.AsrsHubNameHeader).First();
            context.Category = request.Headers.GetValues(Constants.AsrsCategory).First();
            context.Event = request.Headers.GetValues(Constants.AsrsEvent).First();
            context.UserId = request.Headers.GetValues(Constants.AsrsUserId).FirstOrDefault();
            context.Query = GetQueryDictionary(request.Headers.GetValues(Constants.AsrsClientQueryString).FirstOrDefault());
            context.Claims = GetClaimDictionary(request.Headers.GetValues(Constants.AsrsUserClaims));
            context.Headers = GetHeaderDictionary(request);

            return true;
        }

        private IDictionary<string, string> GetQueryDictionary(string queryString)
        {
            if (string.IsNullOrEmpty(queryString))
            {
                return default;
            }

            // The query string looks like "?key1=value1&key2=value2"
            var queryArray = queryString.TrimStart('?').Split('&');
            return queryArray.Select(p => p.Split('=')).ToDictionary(p => p[0], p => p[1]);
        }

        private IDictionary<string, string> GetClaimDictionary(IEnumerable<string> claims)
        {
            return claims?.Select(p => p.Split(':')).ToDictionary(p => p[0], p => p[1].Trim());
        }

        private IDictionary<string, string> GetHeaderDictionary(HttpRequestMessage request)
        {
            return request.Headers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString(), StringComparer.OrdinalIgnoreCase);
        }

        private void AssertTypeMatch(int messageType, int expectedMessageType)
        {
            if (messageType != expectedMessageType)
            {
                throw new FailedRouteEventException($"Type in message doesn't match the expected type: {expectedMessageType}");
            }
        }
    }
}
