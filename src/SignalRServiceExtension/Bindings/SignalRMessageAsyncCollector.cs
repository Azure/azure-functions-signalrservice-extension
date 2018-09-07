// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    public class SignalRMessageAsyncCollector : IAsyncCollector<SignalRMessage>
    {
        private readonly IAzureSignalRSender client;
        private readonly string hubName;

        internal SignalRMessageAsyncCollector(IAzureSignalRSender client, string hubName)
        {
            this.client = client;
            this.hubName = hubName;
        }
        
        public Task AddAsync(SignalRMessage item, CancellationToken cancellationToken = default(CancellationToken))
        {
            var data = new SignalRData
            {
                Target = item.Target,
                Arguments = item.Arguments
            };
            return client.SendToAll(hubName, data);
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