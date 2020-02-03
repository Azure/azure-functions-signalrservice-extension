// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    public class SignalRConnectionInfoV2
    {
        public SignalRConnectionInfo NegotiateResponse;
        public Exception Exception;

        public SignalRConnectionInfoV2(SignalRConnectionInfo negotiateResponse, Exception exception = null)
        {
            NegotiateResponse = negotiateResponse;
            Exception = exception;
        }
    }
}