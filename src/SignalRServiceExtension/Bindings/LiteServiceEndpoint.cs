// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.SignalR;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    /// <summary>
    /// Represents a Azure SignalR Service endpoint, a lite version of <see cref="ServiceEndpoint"/> for endpoints routing.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    internal class LiteServiceEndpoint
    {
        public EndpointType EndpointType { get; set; }

        public string Name { get; set; }

        public string Endpoint { get; set; }

        public bool Online { get; set; }
    }
}