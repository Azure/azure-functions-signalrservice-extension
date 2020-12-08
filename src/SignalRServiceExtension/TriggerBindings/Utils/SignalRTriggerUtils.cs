// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.WebJobs.Extensions.SignalRService.TriggerBindings.Utils;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{ 
    internal static class SignalRTriggerUtils
    {
        private const string AccessKeyProperty = "accesskey";
        private const string CommaSeparator = ",";
        private static readonly char[] PropertySeparator = { ';' };
        private static readonly char[] KeyValueSeparator = { '=' };
        private static readonly char[] QuerySeparator = { '&' };
        private static readonly char[] HeaderSeparator = { ',' };
        private static readonly string[] ClaimsSeparator = { ": " };

        public static string GetAccessKey(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                return null;
            }

            var properties = connectionString.Split(PropertySeparator, StringSplitOptions.RemoveEmptyEntries);
            if (properties.Length < 2)
            {
                throw new ArgumentException("Connection string missing required properties endpoint and accessKey.");
            }

            foreach (var property in properties)
            {
                var kvp = property.Split(KeyValueSeparator, 2);
                if (kvp.Length != 2) continue;

                var key = kvp[0].Trim();
                if (string.Equals(key, AccessKeyProperty, StringComparison.OrdinalIgnoreCase))
                {
                    return kvp[1].Trim();
                }
            }

            throw new ArgumentException("Connection string missing required properties accessKey.");
        }

        public static IDictionary<string, string> GetQueryDictionary(string queryString)
        {
            if (string.IsNullOrEmpty(queryString))
            {
                return default;
            }

            // The query string looks like "?key1=value1&key2=value2"
            var queries = QueryHelpers.ParseQuery(queryString);
            return queries.ToDictionary(x => x.Key, x => x.Value.ToString());
        }

        public static IDictionary<string, string> GetClaimDictionary(string claims)
        {
            if (string.IsNullOrEmpty(claims))
            {
                return default;
            }

            // The claim string looks like "a: v, b: v"
            return claims.Split(HeaderSeparator, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Split(ClaimsSeparator, StringSplitOptions.RemoveEmptyEntries)).Where(l => l.Length == 2)
                .GroupBy(s => s[0].Trim(), (k, g) => new KeyValuePair<string, string>(k, g.Select(gk => gk[1].Trim()).FirstOrDefault()))
                .ToDictionary(x => x.Key, x => x.Value);
        }

        public static IReadOnlyList<string> GetSignatureList(string signatures)
        {
            if (string.IsNullOrEmpty(signatures))
            {
                return default;
            }

            return signatures.Split(HeaderSeparator, StringSplitOptions.RemoveEmptyEntries);
        }

        public static IDictionary<string, string> GetHeaderDictionary(HttpRequestHeaders headers)
        {
            return headers.ToDictionary(x => x.Key, x => string.Join(CommaSeparator, x.Value.ToArray()), StringComparer.OrdinalIgnoreCase);
        }

        public static JObject ToJObject(this InvocationContext invocationContext)
        {
            return JObject.Parse(JsonConvert.SerializeObject(invocationContext, new StringValuesConverter()));
        }
    }
}
