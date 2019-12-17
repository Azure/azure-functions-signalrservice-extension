namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    public class InvocationContext
    {
        public ConnectionContext Context { get; set; }

        public ISignalRServerlessMessage Data { get; set; }

        public class ConnectionContext
        {
            public string HubName { get; set; }

            public string ConnectionId { get; set; }

            public string UserId { get; set; }
        }
    }
}
