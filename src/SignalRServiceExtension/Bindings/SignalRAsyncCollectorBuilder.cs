// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal class SignalRAsyncCollectorBuilder<T> : IAsyncConverter<SignalRAttribute, IAsyncCollector<T>>
    {
        public async Task<IAsyncCollector<T>> ConvertAsync(SignalRAttribute input, CancellationToken cancellationToken)
        {
            var client = await Utils.GetAzureSignalRClient(input.ConnectionStringSetting, input.HubName);
            return new SignalRAsyncCollector<T>(client);
        }
    }
}