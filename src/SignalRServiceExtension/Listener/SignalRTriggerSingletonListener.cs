using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    public class SignalRTriggerSingletonListener : IListener, IEventProcessorFactory
    {
        private readonly ListenerFactoryContext _context;
        private readonly EventProcessorHost _eventProcessorHost;
        private readonly SignalROptions _options;
        private readonly ILogger _logger;
        private bool _started;
        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1);
        private readonly SignalRTriggerListenerDispatcher _dispatcher = new SignalRTriggerListenerDispatcher();

        public SignalRTriggerSingletonListener(ListenerFactoryContext context, EventProcessorHost eventProcessorHost, SignalROptions options, ILogger logger)
        {
            _context = context;
            _eventProcessorHost = eventProcessorHost;
            _options = options;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellation)
        {
            if (!_started)
            {
                await _semaphoreSlim.WaitAsync();
                try
                {
                    if (!_started)
                    {
                        await _eventProcessorHost.RegisterEventProcessorFactoryAsync(this,
                            _options.EventProcessorOptions);
                        _started = true;

                    }
                }
                finally
                {
                    _semaphoreSlim.Release();
                }
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_started)
            {
                await _semaphoreSlim.WaitAsync();
                try
                {
                    if (_started)
                    {
                        await _eventProcessorHost.UnregisterEventProcessorAsync();
                        _started = false;
                    }
                }
                finally
                {
                    _semaphoreSlim.Release();
                }
            }
        }

        public void Cancel()
        {
            StopAsync(CancellationToken.None).Wait();
        }

        public void Dispose()
        {

        }

        public IEventProcessor CreateEventProcessor(PartitionContext context)
        {
            return new EventProcessor(_context, _options, _logger, _dispatcher);
        }

        /// <summary>
        /// Wrapper for un-mockable checkpoint APIs to aid in unit testing
        /// </summary>
        public interface ICheckpointer
        {
            Task CheckpointAsync(PartitionContext context);
        }

        public class EventProcessor : IEventProcessor, IDisposable, ICheckpointer
        {
            private readonly CancellationTokenSource _cts = new CancellationTokenSource();
            private readonly ListenerFactoryContext _context;
            private readonly ITriggeredFunctionExecutor _executor;
            private readonly SignalROptions _options;
            private readonly ILogger _logger;
            private bool _disposed = false;
            private ICheckpointer _checkpointer;
            private readonly SignalRTriggerListenerDispatcher _dispatcher;

            public EventProcessor(ListenerFactoryContext context, SignalROptions options, ILogger logger, SignalRTriggerListenerDispatcher dispatcher, ICheckpointer checkpointer = null)
            {
                _context = context;
                _executor = context.Executor;
                _options = options;
                _logger = logger;
                _dispatcher = dispatcher;
                _checkpointer = checkpointer ?? this;
            }

            public Task OpenAsync(PartitionContext context)
            {
                return Task.CompletedTask;
            }

            public Task CloseAsync(PartitionContext context, CloseReason reason)
            {
                _cts.Cancel();
                return Task.CompletedTask;
            }

            public Task ProcessErrorAsync(PartitionContext context, Exception error)
            {
                string errorMessage = $"Error processing event from Partition Id:{context.PartitionId}, Owner:{context.Owner}, EventHubPath:{context.EventHubPath}";
                _logger.LogError(error, errorMessage);

                return Task.CompletedTask;
            }

            public async Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> messages)
            {
                if (messages.Any())
                {
                    // We set checkpoint at first, so the process can garantee each message to be consumed at most once
                    await _checkpointer.CheckpointAsync(context);
                }

                foreach (var message in messages)
                {
                    if (_cts.IsCancellationRequested)
                    {
                        break;
                    }

                    var input = SignalRTriggerInput.Parse(message);

                    await _dispatcher.DispatchListener(input, _cts);

                    // Dispose message to help with memory pressure. If this is missed, the finalizer thread will still get them.
                    message.Dispose();
                }
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!_disposed)
                {
                    if (disposing)
                    {
                        _cts.Dispose();
                    }

                    _disposed = true;
                }
            }

            public void Dispose()
            {
                Dispose(true);
            }

            async Task ICheckpointer.CheckpointAsync(PartitionContext context)
            {
                await context.CheckpointAsync();
            }
        }
    }
}
