// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.SignalR;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal static class ServiceEndpointExtensions
    {
        private const string FakeAccessKey = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        public static LiteServiceEndpoint ToLiteServiceEndpoint(this ServiceEndpoint e)
        {
            return new LiteServiceEndpoint
            {
                EndpointType = e.EndpointType,
                Name = e.Name,
                Endpoint = e.Endpoint,
                Online = e.Online
            };
        }

        public static ServiceEndpoint ToEqualServiceEndpoint(this LiteServiceEndpoint e)
        {
            var connectionString = $"Endpoint={e.Endpoint};AccessKey={FakeAccessKey};Version=1.0;";
            return new ServiceEndpoint(connectionString, e.EndpointType, e.Name);
        }
    }
}