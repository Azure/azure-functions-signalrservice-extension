using Newtonsoft.Json;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    public class SignalRConnectionInfo
    {
        [JsonProperty("endpoint")]
        public string Endpoint { get; set; }
        [JsonProperty("accessKey")]
        public string AccessKey { get; set; }
    }
}