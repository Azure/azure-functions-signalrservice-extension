// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

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

        public async Task<HttpResponseMessage> ProcessAsync(HttpRequestMessage req)
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

            if (!TryGetConnectionContext(req, out var connectionContext))
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }

            var hubName = connectionContext.Hub;
            // TODO: select out executor that match the pattern
            if (_executors.TryGetValue(hubName, out var executor))
            {
                var payload = new ReadOnlySequence<byte>(await req.Content.ReadAsByteArrayAsync());
                var messageParser = MessageParser.GetParser(contentType);

                if (messageParser.TryParseMessage(ref payload, out var message))
                {
                    switch (message)
                    {
                        case InvocationMessage invocationMessage:
                        {
                            var invocationContext = (InvocationContext) connectionContext;
                            invocationContext.Arguments = invocationMessage.Arguments;
                            return await executor.ExecuteInvocation(messageParser.Protocol, invocationContext,
                                invocationMessage.Target, invocationMessage.InvocationId);
                        }
                        case OpenConnectionMessage openConnectionMessage:
                        {
                            return await executor.ExecuteOpenConnection((OpenConnectionContext) connectionContext);
                        }
                        case CloseConnectionMessage closeConnectionMessage:
                        {
                            var closeConnectionContext = (CloseConnectionContext) connectionContext;
                            closeConnectionContext.ErrorMessage = closeConnectionMessage.Error;
                            return await executor.ExecuteCloseConnection(closeConnectionContext);
                        }
                        default:
                            return new HttpResponseMessage(HttpStatusCode.BadRequest);
                    }
                }

                return new HttpResponseMessage(HttpStatusCode.BadRequest);
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

        private bool TryGetConnectionContext(HttpRequestMessage request, out Context context)
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
            context.ConnectionId = request.Headers.GetValues(Constants.AsrsConnectionIdHeader).FirstOrDefault();
            context.Hub = request.Headers.GetValues(Constants.AsrsHubNameHeader).FirstOrDefault();
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
            if (queryString.StartsWith("?"))
            {
                queryString = queryString.Substring(1);
            }

            var queryArray = queryString.Split('&');
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
    }
}
