// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    [JsonObject]
    public class SignalRGroupAction
    {
        [JsonProperty("userId"), JsonRequired]
        public string UserId { get; set; }
        [JsonProperty("groupName"), JsonRequired]
        public string GroupName { get; set; }
        [JsonProperty("action"), JsonRequired]
        public GroupAction Action { get; set; }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum GroupAction
    {
        [EnumMember(Value = "add")]
        Add,
        [EnumMember(Value = "remove")]
        Remove
    }
}