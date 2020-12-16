// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal class SignalRCollectorBuilder<T> : IConverter<SignalRAttribute, IAsyncCollector<T>>
    {
        public IAsyncCollector<T> Convert(SignalRAttribute attribute)
        {
            var client = Utils.GetAzureSignalRClient(attribute.ConnectionStringSetting, attribute.HubName);
            return new SignalRAsyncCollector<T>(client);
        }
    }
}