﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.SignalR;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class LiteServiceEndpoint
    {
        public string ConnectionString { get; set; }

        public EndpointType EndpointType { get; set; }

        public string Name { get; set; }

        public string Endpoint { get; set; }

        public bool Online { get; set; }

        public static LiteServiceEndpoint FromServiceEndpoint(ServiceEndpoint e)
        {
            return new LiteServiceEndpoint
            {
                ConnectionString = e.ConnectionString,
                EndpointType = e.EndpointType,
                Name = e.Name,
                Endpoint = e.Endpoint,
                Online = e.Online
            };
        }

        public ServiceEndpoint ToServiceEndpoint()
        {
            return new ServiceEndpoint(ConnectionString, EndpointType, Name);
        }
    }
}