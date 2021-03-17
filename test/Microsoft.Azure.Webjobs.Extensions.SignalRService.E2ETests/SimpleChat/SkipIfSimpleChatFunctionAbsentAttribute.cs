// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Testing.xunit;
using static Microsoft.Azure.Webjobs.Extensions.SignalRService.E2ETests.SimpleChatTests;

namespace Microsoft.Azure.Webjobs.Extensions.SignalRService.E2ETests.SimpleChat
{
    public class SkipIfSimpleChatFunctionAbsentAttribute : Attribute, ITestCondition
    {
        public bool IsMet => BaseUrls.Data.Count() == 0;

        public string SkipReason => "Simple-chat functions base urls are not configured.";
    }
}