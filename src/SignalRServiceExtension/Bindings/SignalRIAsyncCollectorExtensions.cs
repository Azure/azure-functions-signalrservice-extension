// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;

namespace Microsoft.Azure.WebJobs
{
    public static class SignalRIAsyncCollectorExtensions
    {
        public static IHubClients GetHubClients(this IAsyncCollector<SignalRMessage> collector)
        {
            return new CollectorHubClients(collector);
        }

        public static IUserGroupManager GetHubGroups(this IAsyncCollector<SignalRGroupAction> collector)
        {
            return new CollectorUserGroupManager(collector);
        }

        private class CollectorClientProxy : IClientProxy
        {
            private readonly IAsyncCollector<SignalRMessage> _collector;
            private IEnumerable<SignalRMessage> _destinations;

            public CollectorClientProxy(IAsyncCollector<SignalRMessage> collector, SignalRMessage destination)
                : this(collector, new[] { destination })
            {
            }

            public CollectorClientProxy(IAsyncCollector<SignalRMessage> collector, IEnumerable<SignalRMessage> destinations)
            {
                _collector = collector;
                _destinations = destinations;
            }

            public async Task SendCoreAsync(string method, object[] args, CancellationToken cancellationToken = default)
            {
                foreach (var destination in _destinations)
                {
                    await _collector.AddAsync(new SignalRMessage
                    {
                        GroupName = destination.GroupName,
                        UserId = destination.UserId,
                        Target = method,
                        Arguments = args,
                    }, cancellationToken);
                }
            }
        }

        private class CollectorHubClients : IHubClients
        {
            private readonly IAsyncCollector<SignalRMessage> _collector;

            public CollectorHubClients(IAsyncCollector<SignalRMessage> collector)
            {
                _collector = collector;
            }

            public IClientProxy All => new CollectorClientProxy(_collector, new SignalRMessage());

            public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds)
            {
                throw new NotImplementedException();
            }

            public IClientProxy Client(string connectionId)
            {
                throw new NotImplementedException();
            }

            public IClientProxy Clients(IReadOnlyList<string> connectionIds)
            {
                throw new NotImplementedException();
            }

            public IClientProxy Group(string groupName)
            {
                return new CollectorClientProxy(_collector, new SignalRMessage
                {
                    GroupName = groupName
                });
            }

            public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds)
            {
                throw new NotImplementedException();
            }

            public IClientProxy Groups(IReadOnlyList<string> groupNames)
            {
                return new CollectorClientProxy(_collector, groupNames.Select(groupName => new SignalRMessage
                {
                    GroupName = groupName
                }));
            }

            public IClientProxy User(string userId)
            {
                return new CollectorClientProxy(_collector, new SignalRMessage
                {
                    UserId = userId
                });
            }

            public IClientProxy Users(IReadOnlyList<string> userIds)
            {
                return new CollectorClientProxy(_collector, userIds.Select(userId => new SignalRMessage
                {
                    UserId = userId 
                }));
            }
        }

        private class CollectorUserGroupManager : IUserGroupManager
        {
            private readonly IAsyncCollector<SignalRGroupAction> _collector;

            public CollectorUserGroupManager(IAsyncCollector<SignalRGroupAction> collector)
            {
                _collector = collector;
            }

            public Task AddToGroupAsync(string userId, string groupName, CancellationToken cancellationToken = default)
            {
                return _collector.AddAsync(new SignalRGroupAction
                {
                    UserId = userId,
                    GroupName = groupName,
                    Action = GroupAction.Add
                }, cancellationToken);
            }

            public Task RemoveFromGroupAsync(string userId, string groupName, CancellationToken cancellationToken = default)
            {
                return _collector.AddAsync(new SignalRGroupAction
                {
                    UserId = userId,
                    GroupName = groupName,
                    Action = GroupAction.Remove
                }, cancellationToken);
            }
        }
    }
}
