using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal class DefaultRequestResolver : IRequestResolver
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
            context.Query = SignalRTriggerUtils.GetQueryDictionary(request.Headers.GetValues(Constants.AsrsClientQueryString).FirstOrDefault());
            context.Claims = SignalRTriggerUtils.GetClaimDictionary(request.Headers.GetValues(Constants.AsrsUserClaims).FirstOrDefault());
            context.Headers = SignalRTriggerUtils.GetHeaderDictionary(request);

            return true;
        }
    }
}
