// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    public class SignalRGroupAction
    {
        public string UserId { get; set; }
        public string GroupName { get; set; }
        public GroupAction Action { get; set; }
    }

    public enum GroupAction
    {
        Add,
        Remove
    }
}