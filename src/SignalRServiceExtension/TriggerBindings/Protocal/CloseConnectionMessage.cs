using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    public class CloseConnectionMessage : ISignalRServerlessMessage
    {
        public int Type { get; set; }

        public string ConnectionId { get; set; }
    }
}
