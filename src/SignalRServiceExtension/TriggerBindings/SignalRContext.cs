using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    public class SignalRContext
    {
        public string HubName { get; set; }

        public string UserId { get; set; }

        public Dictionary<string, string> Claims { get; set; }
    }
}
