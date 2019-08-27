using Microsoft.Azure.WebJobs.Host.Listeners;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Executors;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal class SignalRListener: IListener
    {
        public ITriggeredFunctionExecutor Executor { private set; get; }

        private readonly SignalRConfigProvider _configProvider;
        private readonly string _hubName;

        public SignalRListener(ITriggeredFunctionExecutor executor, SignalRConfigProvider configProvider, string hubName)
        {
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
            _hubName = hubName ?? throw new ArgumentNullException(nameof(hubName));
            Executor = executor ?? throw new ArgumentNullException(nameof(executor));
        }

        public void Dispose()
        {
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _configProvider.AddListener(_hubName, this);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public void Cancel()
        {
        }
    }
}
