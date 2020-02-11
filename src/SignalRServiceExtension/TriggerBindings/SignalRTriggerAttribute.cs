// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.WebJobs.Description;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    [AttributeUsage(AttributeTargets.ReturnValue | AttributeTargets.Parameter)]
    [Binding]
    public class SignalRTriggerAttribute : Attribute
    {
        [AutoResolve]
        public string HubName { get; set; }

        [AutoResolve]
        public string Target { get; set; }

        public string[] ParameterNames { get; set; }
    }
}
