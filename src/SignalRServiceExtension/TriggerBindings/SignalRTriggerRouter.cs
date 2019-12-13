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
        // Header:       X-ASRS-Serverless-ConnectionId
        //               X-ASRS-Serverless-UserId
        // Content-Type: application/json / application/x-msgpack
        // Body:         Payload
        public async Task<HttpResponseMessage> ProcessAsync(HttpRequestMessage req)
        {
            var path = req.RequestUri.AbsolutePath;
            var contentType = req.Content.Headers.ContentType.MediaType;
            if (!ValidateContentType(contentType))
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }

            var connectionId = req.Headers.GetValues(Constants.ConnectionIdHeader).FirstOrDefault();

            if (TryGetHubName(path, out var hubName))
            {
                if (_executors.TryGetValue(hubName, out var executor))
                {
                    var payload = new ReadOnlySequence<byte>(await req.Content.ReadAsByteArrayAsync());
                    var messageParser = MessageParser.GetParser(contentType);
                    

                    while (messageParser.TryParseMessage(ref payload, out var message))
                    {
                        await executor.ExecuteMethod(new InvocationContext.ConnectionContext
                        {
                            ConnectionId = req.Content.Headers.GetValues(Constants.ConnectionIdHeader).FirstOrDefault(),
                            UserId = req.Content.Headers.GetValues(Constants.UserIdHeader).FirstOrDefault(),
                        }, message);
                    }

                    return new HttpResponseMessage(HttpStatusCode.OK);
                }
                // No target hub in functions
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }
            // Request not meet convention
            return new HttpResponseMessage(HttpStatusCode.BadRequest);
        }

        private bool TryGetHubName(string path, out string hubName)
        {
            // The url should be /runtime/webhooks/signalr/{hub}
            var paths = path.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (!ValidateUri(paths))
            {
                hubName = null;
                return false;
            }

            hubName = paths[3];
            return true;
        }

        private bool ValidateUri(string[] paths)
        {
            if (paths.Length != 4)
            {
                return false;
            }

            if (paths[2] != "signalr")
            {
                return false;
            }

            return true;
        }

        private bool ValidateContentType(string contentType)
        {
            return contentType == Constants.JsonContentType || contentType == Constants.MessagepackContentType;
        }
    }
}
