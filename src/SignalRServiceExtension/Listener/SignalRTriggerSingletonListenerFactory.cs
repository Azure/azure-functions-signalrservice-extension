using System;
using Microsoft.Azure.EventHubs.Processor;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    // A function app scoped factory, managing dictionary [host -> listener].
    public class SignalRTriggerSingletonListenerFactory
    {
        private static readonly SignalRTriggerSingletonListenerFactory _factory = new SignalRTriggerSingletonListenerFactory();

        private readonly SignalRTriggerListenerDispatcher _dispatcher = new SignalRTriggerListenerDispatcher();
        private readonly ConcurrentDictionary<string, SignalRTriggerSingletonListener> _listeners =
            new ConcurrentDictionary<string, SignalRTriggerSingletonListener>();

        static SignalRTriggerSingletonListenerFactory()
        {
        }

        public static SignalRTriggerSingletonListenerFactory Instance => _factory;

        public SignalRTriggerSingletonListener CreateListener(EventProcessorHost eventProcessorHost, ListenerFactoryContext context, Type attributeType, SignalROptions options, ILogger logger)
        {
            _dispatcher.RegisterFunction(context.Descriptor.Id, attributeType, context);
            var listener = _listeners.GetOrAdd(context.Descriptor.Id, new SignalRTriggerSingletonListener(
                eventProcessorHost, _dispatcher, options, logger));
            return listener;
        }
    }
}
