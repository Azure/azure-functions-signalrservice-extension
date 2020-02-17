// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal static class InvocationContextHelper
    {
        public static bool TryGetInvocationContext(HttpRequestMessage request, out InvocationContext context)
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
            context.Claims = GetClaimDictionary(request.Headers.GetValues(Constants.AsrsUserClaims).FirstOrDefault());
            context.Headers = GetHeaderDictionary(request);

            return true;
        }

        private static IDictionary<string, string> GetQueryDictionary(string queryString)
        {
            if (string.IsNullOrEmpty(queryString))
            {
                return default;
            }

            // The query string looks like "?key1=value1&key2=value2"
            var queryArray = queryString.TrimStart('?').Split('&');
            return queryArray.Select(p => p.Split('=')).ToDictionary(p => p[0], p => p[1]);
        }

        private static IDictionary<string, string> GetClaimDictionary(string claims)
        {
            return claims?.Split(',').Select(p => p.Split(new []{": "}, StringSplitOptions.None)).ToDictionary(p => p[0].Trim(), p => p[1].Trim());
        }

        private static IDictionary<string, string> GetHeaderDictionary(HttpRequestMessage request)
        {
            return request.Headers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.FirstOrDefault(), StringComparer.OrdinalIgnoreCase);
        }
    }
}
