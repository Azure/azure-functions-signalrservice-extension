// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal interface IAzureSignalRSender
    {
        Task SendToAll(string hubName, SignalRData data);
        Task SendToUser(string hubName, string userId, SignalRData data);
        Task SendToGroup(string hubName, string group, SignalRData data);
        Task AddUser(string hubName, string userId, string group);
        Task RemoveUser(string hubName, string userId, string group);
    }
}