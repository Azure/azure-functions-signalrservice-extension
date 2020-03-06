// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{ 
    internal static class SignalRTriggerUtils
    {
        private const string AccessKeyProperty = "accesskey";
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
            var queryArray = queryString.TrimStart('?').Split(QuerySeparator, StringSplitOptions.RemoveEmptyEntries);
            return queryArray.Select(p => p.Split(KeyValueSeparator)).ToDictionary(p => p[0].Trim(), p => p[1].Trim());
        }

        public static IDictionary<string, string> GetClaimDictionary(string claims)
        {
            if (string.IsNullOrEmpty(claims))
            {
                return default;
            }

            // The claim string looks like "a= v, b= v"
            return claims.Split(HeaderSeparator)
                .Select(p => p.Split(ClaimsSeparator, StringSplitOptions.RemoveEmptyEntries))
                .ToDictionary(p => p[0].Trim(), p => p[1].Trim());
        }

        public static IReadOnlyList<string> GetSignatureList(string signatures)
        {
            if (string.IsNullOrEmpty(signatures))
            {
                return default;
            }

            return signatures.Split(HeaderSeparator, StringSplitOptions.RemoveEmptyEntries);
        }

        public static IDictionary<string, string> GetHeaderDictionary(HttpRequestMessage request)
        {
            return request.Headers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.FirstOrDefault(), StringComparer.OrdinalIgnoreCase);
        }
    }
}
