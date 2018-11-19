using System;
using Microsoft.Azure.EventHubs.Processor;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using Microsoft.Azure.WebJobs.Extensions.SignalRService.Protocols;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    // A function app scoped factory, managing dictionary [host -> listener].
    public class SignalRTriggerSharedListenerFactory
    {
        private readonly ISignalRExtensionProtocols _protocols = new SignalRExtensionProtocols();
        private readonly ISignalRTriggerListenerDispatcher _dispatcher;
        private readonly ConcurrentDictionary<string, SignalRTriggerSharedListener> _listeners =
            new ConcurrentDictionary<string, SignalRTriggerSharedListener>();

        public SignalRTriggerSharedListenerFactory()
        {
            _dispatcher = new SignalRTriggerListenerDispatcher(_protocols);
        }

        public static SignalRTriggerSharedListenerFactory Instance { get; } = new SignalRTriggerSharedListenerFactory();

        public SignalRTriggerSharedListener CreateListener(EventProcessorHost eventProcessorHost, ListenerFactoryContext context, Type attributeType, string hubName, SignalROptions options, ILogger logger, string target = null)
        {
            _dispatcher.RegisterFunction(context.Descriptor.Id, attributeType, hubName, context, target);
            var listener = _listeners.GetOrAdd(eventProcessorHost.HostName, new SignalRTriggerSharedListener(
                eventProcessorHost, _dispatcher, options, logger));
            return listener;
        }
    }
}
