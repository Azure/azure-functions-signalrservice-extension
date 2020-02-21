// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Azure.WebJobs.Extensions.SignalRService.Exceptions;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal class SignalRTriggerDispatcher : ISignalRTriggerDispatcher
    {
        private readonly Dictionary<string, SignalRHubMethodExecutor> _executors =
            new Dictionary<string, SignalRHubMethodExecutor>(StringComparer.OrdinalIgnoreCase);
        private readonly IRequestResolver _resolver;

        public SignalRTriggerDispatcher(IRequestResolver resolver = null)
        {
            _resolver = resolver ?? new DefaultRequestResolver();
        }

        public void Map((string hubName, string category, string @event) key, ExecutionContext executor)
        {
            if (!_executors.TryGetValue(key.hubName, out var hubExecutor))
            {
                hubExecutor = new SignalRHubMethodExecutor(key.hubName, _resolver);
                _executors.Add(key.hubName, hubExecutor);
            }

            hubExecutor.Map((key.category, key.@event), executor);
        }

        public async Task<HttpResponseMessage> ExecuteAsync(HttpRequestMessage req, CancellationToken token = default)
        {
            // TODO: More details about response
            if (!_resolver.ValidateContentType(req))
            {
                return new HttpResponseMessage(HttpStatusCode.UnsupportedMediaType);
            }

            if (!TryGetDispatchingKey(req, out var key))
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }
            
            // TODO: select out executor that match the pattern
            if (_executors.TryGetValue(key.hub, out var executor))
            {
                try
                {
                    if (key.category == Constants.Category.Connections)
                    {
                        if (key.@event == Constants.Events.Connect)
                        {
                            return await executor.ExecuteOpenConnection(req);
                        }

                        if (key.@event == Constants.Events.Disconnect)
                        {
                            return await executor.ExecuteCloseConnection(req);
                        }

                        throw new FailedRouteEventException($"{key.@event} is not a supported event for connections");
                    }

                    if (key.category == Constants.Category.Messages)
                    {
                        return await executor.ExecuteInvocation(req);
                    }

                    throw new FailedRouteEventException($"{key.category} is not a supported category");
                }
                catch (SignalRTriggerException ex)
                {
                    return new HttpResponseMessage(HttpStatusCode.BadRequest)
                    {
                        ReasonPhrase = ex.Message
                    };
                }
            }

            // No target hub in functions
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        private bool TryGetDispatchingKey(HttpRequestMessage request, out (string hub, string category, string @event) key)
        {
            key.hub = request.Headers.GetValues(Constants.AsrsHubNameHeader).First();
            key.category = request.Headers.GetValues(Constants.AsrsCategory).First();
            key.@event = request.Headers.GetValues(Constants.AsrsEvent).First();
            return !string.IsNullOrEmpty(key.hub) &&
                   !string.IsNullOrEmpty(key.category) &&
                   !string.IsNullOrEmpty(key.@event);
        }
    }
}
