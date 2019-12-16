using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    public class SignalRTriggerRouter
    {
        private readonly Dictionary<string, SignalRHubMethodExecutor> _executors = new Dictionary<string, SignalRHubMethodExecutor>(StringComparer.OrdinalIgnoreCase);

        internal void AddListener((string hubName, string methodName) key, SignalRListener listener)
        {
            if (!_executors.TryGetValue(key.hubName, out var executor))
            {
                executor = new SignalRHubMethodExecutor(key.hubName);
                _executors.Add(key.hubName, executor);
            }
            executor.AddListener(key.methodName, listener);
        }

        // The request should meet the convention
        // Header:       X-ASRS-ConnectionId
        //               X-ASRS-UserId
        //               X-ASRS-HubName
        // Content-Type: application/json / application/x-msgpack
        // Body:         Payload
        public async Task<HttpResponseMessage> ProcessAsync(HttpRequestMessage req)
        {
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
            if (_executors.TryGetValue(hubName, out var executor))
            {
                var payload = new ReadOnlySequence<byte>(await req.Content.ReadAsByteArrayAsync());
                var messageParser = MessageParser.GetParser(contentType);
                
                if (messageParser.TryParseMessage(ref payload, out var message))
                {
                    var result = await executor.ExecuteMethod(connectionContext, message);
                }

                return new HttpResponseMessage(HttpStatusCode.OK);
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
