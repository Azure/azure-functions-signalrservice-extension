// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    public interface ISignalRConnectionInfoConfigurer
    {
        Func<AccessTokenResult, HttpRequest, SignalRConnectionDetail, SignalRConnectionDetail> Configure { get; set; }
    }
}