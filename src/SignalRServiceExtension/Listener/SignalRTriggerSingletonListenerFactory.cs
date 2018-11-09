using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    public class SignalRTriggerSingletonListenerFactory
    {
        public SignalRTriggerSingletonListenerFactory()
        {

        }

        public SignalRTriggerSingletonListenerFactory CreateListener()
        {
            return new SignalRTriggerSingletonListenerFactory();
        }
    }
}
