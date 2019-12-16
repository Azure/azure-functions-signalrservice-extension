using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    public class SignalRTriggerRouter
    {
        private readonly Dictionary<string, SignalRHubMethodExecutor> _executors = new Dictionary<string, SignalRHubMethodExecutor>(StringComparer.OrdinalIgnoreCase);
        private readonly IHubProtocol _jsonHubProtocol = new JsonHubProtocol();
        private readonly IHubProtocol _messagePackHubProtocol = new MessagePackHubProtocol();

        internal void AddRoute((string hubName, string methodName) key, ITriggeredFunctionExecutor executor)
        {
            if (!_executors.TryGetValue(key.hubName, out var hubExecutor))
            {
                hubExecutor = new SignalRHubMethodExecutor(key.hubName);
                _executors.Add(key.hubName, hubExecutor);
            }
            hubExecutor.AddTarget(key.methodName, executor);
        }

        // The request should meet the convention
        // Header:       X-ASRS-ConnectionId
        //               X-ASRS-UserId
        //               X-ASRS-HubName
        // Content-Type: application/json / application/x-msgpack
        // Body:         Payload
        public async Task<HttpResponseMessage> ProcessAsync(HttpRequestMessage req)
        {
            // TODO: More details about response
            var contentType = req.Content.Headers.ContentType.MediaType;
            if (!ValidateContentType(contentType))
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }

            if (!TryGetConnectionContext(req, out var connectionContext))
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }

            var hubName = connectionContext.HubName;
            // TODO: select out executor that match the pattern
            if (_executors.TryGetValue(hubName, out var executor))
            {
                var payload = new ReadOnlySequence<byte>(await req.Content.ReadAsByteArrayAsync());
                var messageParser = MessageParser.GetParser(contentType);
                var protocol = contentType == Constants.JsonContentType ? _jsonHubProtocol : _messagePackHubProtocol;

                if (messageParser.TryParseMessage(ref payload, out var message))
                {
                    switch (message)
                    {
                        case InvocationMessage invocationMessage:
                            return await executor.ExecuteInvocation(protocol, connectionContext, invocationMessage);
                        case OpenConnectionMessage openConnectionMessage:
                            return await executor.ExecuteOpenConnection(connectionContext, openConnectionMessage);
                        case CloseConnectionMessage closeConnectionMessage:
                            return await executor.ExecuteCloseConnection(connectionContext, closeConnectionMessage);
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

        private bool TryGetConnectionContext(HttpRequestMessage request, out InvocationContext.ConnectionContext context)
        {
            if (!request.Headers.Contains(Constants.AsrsHubNameHeader) || !request.Headers.Contains(Constants.AsrsConnectionIdHeader))
            {
                context = null;
                return false;
            }

            context = new InvocationContext.ConnectionContext();
            context.ConnectionId = request.Headers.GetValues(Constants.AsrsConnectionIdHeader).FirstOrDefault();
            context.HubName = request.Headers.GetValues(Constants.AsrsHubNameHeader).FirstOrDefault();
            if (request.Headers.Contains(Constants.AsrsUserIdHeader))
            {
                context.UserId = request.Headers.GetValues(Constants.AsrsUserIdHeader).FirstOrDefault();
            }
            return true;
        }
    }
}
