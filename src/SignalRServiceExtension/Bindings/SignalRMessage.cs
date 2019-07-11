// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    [JsonObject]
    public class SignalRMessage
    {
        [JsonProperty("connectionId")]
        public string ConnectionId { get; set; }
        [JsonProperty("userId")]
        public string UserId { get; set; }
        [JsonProperty("groupName")]
        public string GroupName { get; set; }
        [JsonProperty("target"), JsonRequired]
        public string Target { get; set; }
        [JsonProperty("arguments"), JsonRequired]
        public object[] Arguments { get; set; }
    }
}