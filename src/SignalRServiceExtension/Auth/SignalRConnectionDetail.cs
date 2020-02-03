// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    public class SignalRConnectionDetail
    {
        public string UserId { get; set; }
        public IList<Claim> Claims { get; set; }
    }
}