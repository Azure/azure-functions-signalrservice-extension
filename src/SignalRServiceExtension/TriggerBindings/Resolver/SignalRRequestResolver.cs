// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Azure.SignalR.Serverless.Protocols;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal class SignalRRequestResolver : IRequestResolver
    {
        public bool ValidateContentType(HttpRequestMessage request)
        {
            var contentType = request.Content.Headers.ContentType.MediaType;
            if (string.IsNullOrEmpty(contentType))
            {
                return false;
            }
            return contentType == Constants.JsonContentType || contentType == Constants.MessagePackContentType;
        }

        public bool ValidateSignature(HttpRequestMessage request, string accessToken)
        {
            //TODO: Add real signature validation
            return true;
        }

        public bool TryGetInvocationContext(HttpRequestMessage request, out InvocationContext context)
        {
            context = new InvocationContext();
            // Required properties
            context.ConnectionId = request.Headers.GetValues(Constants.AsrsConnectionIdHeader).FirstOrDefault();
            if (string.IsNullOrEmpty(context.ConnectionId))
            {
                return false;
            }
            context.Hub = request.Headers.GetValues(Constants.AsrsHubNameHeader).FirstOrDefault();
            context.Category = request.Headers.GetValues(Constants.AsrsCategory).FirstOrDefault();
            context.Event = request.Headers.GetValues(Constants.AsrsEvent).FirstOrDefault();
            // Optional properties
            if (request.Headers.TryGetValues(Constants.AsrsUserId, out var values))
            {
                context.UserId = values.FirstOrDefault();
            }
            if (request.Headers.TryGetValues(Constants.AsrsClientQueryString, out values))
            {
                context.Query = SignalRTriggerUtils.GetQueryDictionary(values.FirstOrDefault());
            }
            if (request.Headers.TryGetValues(Constants.AsrsUserClaims, out values))
            {
                context.Claims = SignalRTriggerUtils.GetClaimDictionary(values.FirstOrDefault());
            }
            context.Headers = SignalRTriggerUtils.GetHeaderDictionary(request);

            return true;
        }

        public async Task<(T, IHubProtocol)> GetMessageAsync<T>(HttpRequestMessage request) where T : ServerlessMessage, new()
        {
            var payload = new ReadOnlySequence<byte>(await request.Content.ReadAsByteArrayAsync());
            var messageParser = MessageParser.GetParser(request.Content.Headers.ContentType.MediaType);
            if (!messageParser.TryParseMessage(ref payload, out var message))
            {
                throw new SignalRTriggerException("Parsing message failed");
            }

            return ((T)message, messageParser.Protocol);
        }
    }
}
