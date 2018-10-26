using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal class SignalRCollectorBuilder<T> : IConverter<SignalRAttribute, IAsyncCollector<T>>
    {
        private readonly SignalRConfigProvider _configProvider;

        public SignalRCollectorBuilder(SignalRConfigProvider configProvider)
        {
            _configProvider = configProvider;
        }

        public IAsyncCollector<T> Convert(SignalRAttribute attribute)
        {
            var client = _configProvider.GetClient(attribute);
            return new SignalRAsyncCollector<T>(client, attribute.HubName);
        }
    }
}
