// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Azure.SignalR.Serverless.Protocols;
using Microsoft.Azure.WebJobs.Extensions.SignalRService.Exceptions;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal class SignalRDisconnectMethodExecutor: SignalRMethodExecutor
    {
        public SignalRDisconnectMethodExecutor(IRequestResolver resolver, ExecutionContext executionContext): base(resolver, executionContext)
        {
        }

        public override async Task<HttpResponseMessage> ExecuteAsync(HttpRequestMessage request)
        {
            if (!Resolver.TryGetInvocationContext(request, out var context))
            {
                //TODO: More detailed exception
                throw new SignalRTriggerException();
            }
            var (message, _) = await Resolver.GetMessageAsync<CloseConnectionMessage>(request);
            context.Error = message.Error;

            await ExecuteWithAuthAsync(request, ExecutionContext, context);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
