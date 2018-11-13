using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    public class SignalRBaseMessage
    {
        public string Hub { get; set; }
        public string Method { get; set; }
        public object[] Arguments { get; set; }
    }
}
