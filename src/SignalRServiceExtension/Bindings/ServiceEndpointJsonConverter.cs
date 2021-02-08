// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Azure.SignalR;
using Newtonsoft.Json;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal class ServiceEndpointJsonConverter : JsonConverter<ServiceEndpoint>
    {
        public override ServiceEndpoint ReadJson(JsonReader reader, Type objectType, ServiceEndpoint existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            return serializer.Deserialize<LiteServiceEndpoint>(reader).ToEqualServiceEndpoint();
        }

        public override void WriteJson(JsonWriter writer, ServiceEndpoint value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value.ToLiteServiceEndpoint());
        }
    }
}