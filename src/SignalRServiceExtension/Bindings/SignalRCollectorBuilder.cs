// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal class SignalRCollectorBuilder<T> : IConverter<SignalRAttribute, IAsyncCollector<T>>
    {
        private readonly SignalRConfigProvider configProvider;

        public SignalRCollectorBuilder(SignalRConfigProvider configProvider)
        {
            this.configProvider = configProvider;
        }

        public IAsyncCollector<T> Convert(SignalRAttribute attribute)
        {
            var client = configProvider.GetAzureSignalRClient(attribute.ConnectionStringSetting, attribute.HubName);
            return new SignalRAsyncCollector<T>(client, configProvider.logger);
        }
    }
}
