// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService.TriggerBindings.Utils
{
    public class StringValuesConverter: JsonConverter<StringValues>
    {
        public override void WriteJson(JsonWriter writer, StringValues value, JsonSerializer serializer)
        {
            if (value.Count == 1)
            {
                writer.WriteValue(value.ToString());
            }
            else
            {
                serializer.Serialize(writer, value.ToArray());
            }
        }

        public override StringValues ReadJson(JsonReader reader, Type objectType, StringValues existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotSupportedException();
        }

        public override bool CanRead => false;
    }
}
