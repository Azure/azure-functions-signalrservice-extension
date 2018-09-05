// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    public class SignalRMessageAsyncCollector : IAsyncCollector<SignalRMessage>
    {
        private readonly IAzureSignalRClient client;
        private readonly string hubName;

        internal SignalRMessageAsyncCollector(IAzureSignalRClient client, string hubName)
        {
            this.client = client;
            this.hubName = hubName;
        }
        
        public Task AddAsync(SignalRMessage item, CancellationToken cancellationToken = default(CancellationToken))
        {
            return client.SendMessage(hubName, item);
        }

        public Task FlushAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.CompletedTask;
        }
        
        private string FirstOrDefault(params string[] values)
        {
            return values.FirstOrDefault(v => !string.IsNullOrEmpty(v));
        }
    }
}