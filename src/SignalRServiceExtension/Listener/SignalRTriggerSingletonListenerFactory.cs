using System;
using Microsoft.Azure.EventHubs.Processor;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using Microsoft.Azure.WebJobs.Extensions.SignalRService.Protocols;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    // A function app scoped factory, managing dictionary [host -> listener].
    public class SignalRTriggerSingletonListenerFactory
    {
        private readonly ISignalRExtensionProtocols _protocols = new SignalRExtensionProtocols();
        private readonly SignalRTriggerListenerDispatcher _dispatcher;
        private readonly ConcurrentDictionary<string, SignalRTriggerSingletonListener> _listeners =
            new ConcurrentDictionary<string, SignalRTriggerSingletonListener>();

        public SignalRTriggerSingletonListenerFactory()
        {
            _dispatcher = new SignalRTriggerListenerDispatcher(_protocols);
        }

        public static SignalRTriggerSingletonListenerFactory Instance { get; } = new SignalRTriggerSingletonListenerFactory();

        public SignalRTriggerSingletonListener CreateListener(EventProcessorHost eventProcessorHost, ListenerFactoryContext context, Type attributeType, string hubName, SignalROptions options, ILogger logger, string target = null)
        {
            _dispatcher.RegisterFunction(context.Descriptor.Id, attributeType, hubName, context, target);
            var listener = _listeners.GetOrAdd(eventProcessorHost.HostName, new SignalRTriggerSingletonListener(
                eventProcessorHost, _dispatcher, options, logger));
            return listener;
        }
    }
}
