using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Microsoft.Azure.EventHubs.Processor;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    public interface ISignalRTriggerBindingFactory
    {
        ITriggerBinding Create(ParameterInfo parameter, EventProcessorHost host, Type attributeType, SignalROptions options, string hubName, ILogger logger, string target = null);
    }
}
