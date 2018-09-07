// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    public class SignalRMessage
    {
        public IEnumerable<string> UserIds { get; set; } = new List<string>();
        public string Target { get; set; }
        public object[] Arguments { get; set; }
    }
}