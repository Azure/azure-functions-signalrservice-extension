namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    public class SignalRMessage
    {
        public string Target { get; set; }
        public object[] Arguments { get; set; }
    }
}