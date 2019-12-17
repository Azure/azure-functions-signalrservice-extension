using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    public class OpenConnectionMessage : ISignalRServerlessMessage
    {
        [JsonProperty(PropertyName = "type")]
        public int Type { get; set; }
    }
}
