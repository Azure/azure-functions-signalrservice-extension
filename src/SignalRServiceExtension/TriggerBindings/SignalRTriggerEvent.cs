using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService.TriggerBindings
{
    internal class SignalRTriggerEvent
    {
        public SignalRContext Context { get; set; }

        public TaskCompletionSource<SignalRContext> ContextTcs { get; set; }
    }
}
