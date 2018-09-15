// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    public class SignalRMessage
    {
        public string UserId { get; set; }
        public string Target { get; set; }
        public object[] Arguments { get; set; }
    }
}