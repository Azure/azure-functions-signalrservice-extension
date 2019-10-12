namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    public class InvocationContext
    {
        public string HubName { get; set; }

        public ConnectionContext Context { get; set; }

        public InvocationData Data { get; set; }

        public class ConnectionContext
        {
            public string ConnectionId { get; set; }

            public string UserId { get; set; }
        }

        public class InvocationData
        {
            public int Type { get; set; }

            public int InvocationId { get; set; }

            public string Target { get; set; }

            public object[] Arguments { get; set; }
        }
    }
}
