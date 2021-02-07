// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.SignalR;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    /// <summary>
    /// Represents a Azure SignalR Service endpoints, whose members is a subset 
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class LiteServiceEndpoint
    {
        private string _connectionString;

        public EndpointType EndpointType { get; set; }

        public string Name { get; set; }

        public string Endpoint { get; set; }

        public bool Online { get; set; }

        internal static LiteServiceEndpoint FromServiceEndpoint(ServiceEndpoint e)
        {
            return new LiteServiceEndpoint
            {
                _connectionString = e.ConnectionString,
                EndpointType = e.EndpointType,
                Name = e.Name,
                Endpoint = e.Endpoint,
                Online = e.Online
            };
        }

        internal ServiceEndpoint ToServiceEndpoint()
        {
            return new ServiceEndpoint(_connectionString, EndpointType, Name);
        }
    }
}