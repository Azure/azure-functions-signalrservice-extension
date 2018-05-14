using Newtonsoft.Json;

namespace SignalRExtension
{
    public class AzureSignalREndpoint
    {
        [JsonProperty("endpoint")]
        public string Endpoint { get; set; }
        [JsonProperty("accessKey")]
        public string AccessKey { get; set; }
    }
}