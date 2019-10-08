// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal interface IAzureSignalRSender
    {
        Task SendToAll(SignalRData data);
        Task SendToConnection(string connectionId, SignalRData data);
        Task SendToUser(string userId, SignalRData data);
        Task SendToGroup(string group, SignalRData data);
        Task AddUserToGroup(string userId, string groupName);
        Task RemoveUserFromGroup(string userId, string groupName);
        Task AddConnectionToGroup(string connectionId, string groupName);
        Task RemoveConnectionFromGroup(string connectionId, string groupName);
    }
}