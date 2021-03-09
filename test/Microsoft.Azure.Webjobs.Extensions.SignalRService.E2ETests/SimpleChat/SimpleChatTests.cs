// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Azure.Webjobs.Extensions.SignalRService.E2ETests.SimpleChat;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Xunit;
using static Microsoft.Azure.Webjobs.Extensions.SignalRService.E2ETests.Utils;

namespace Microsoft.Azure.Webjobs.Extensions.SignalRService.E2ETests
{
    // To enable tests run in parallel, split them into different class.
    public class SimpleChatTests
    {
        private const string Section = "SimpleChat";
        public static readonly SimpleChatClient Client = new();
        public class BaseUrls : IEnumerable<object[]>
        {
            public static readonly IEnumerable<object[]> Data = from section in UrlConfiguration.GetSection(Section).GetChildren()
                                                                select new object[] { section.Key, section.Value };

            public IEnumerator<object[]> GetEnumerator() => Data.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public class NegotiationTests
        {
            [ConditionalTheory]
            [ClassData(typeof(BaseUrls))]
            [SkipIfSimpleChatFunctionAbsent]
            public async Task Negotiation(string key, string url)
            {
                const string target = nameof(NegotiationTests);
                const int count = 2;
                var users = GenerateRandomUsers(count);
                var messageToSend = key + Guid.NewGuid().ToString();
                var completionSources = new ConcurrentBag<TaskCompletionSource>();
                var tasks = users.Select(async user =>
                {
                    var connectionInfo = await SimpleChatClient.Negotiate(user, url);
                    var connection = CreateHubConnection(connectionInfo.Url, connectionInfo.AccessToken);
                    var taskCompleSource = new TaskCompletionSource();
                    completionSources.Add(taskCompleSource);
                    connection.On(target, (string message) =>
                    {
                        if (message.Equals(messageToSend))
                        {
                            taskCompleSource.SetResult();
                        }
                    });
                    await connection.StartAsync();
                    return connection;
                }).ToArray();
                var connections = await Task.WhenAll(tasks);

                await SimpleChatClient.Send(url, new SignalRMessage
                {
                    Target = target,
                    Arguments = new object[] { messageToSend }
                });

                await Task.WhenAll(completionSources.Select(s => s.Task)).OrTimeout(TimeSpan.FromSeconds(10));

                //clean
                await Task.WhenAll(connections.Select(c => c.DisposeAsync().AsTask()));
            }
        }

        public class ConnectionGroupManagementTest
        {
            [ConditionalTheory]
            [ClassData(typeof(BaseUrls))]
            [SkipIfSimpleChatFunctionAbsent]
            public async Task ConnectionGroupManagement(string key, string url)
            {
                const string target = nameof(ConnectionGroupManagementTest);
                const int count = 2;
                string groupName = Guid.NewGuid().ToString();

                var users = GenerateRandomUsers(count).ToArray();
                var messageToSend = key + Guid.NewGuid().ToString();
                var completionSources = new ConcurrentDictionary<string, TaskCompletionSource>();
                var tasks = users.Select(async user =>
                {
                    var connectionInfo = await SimpleChatClient.Negotiate(user, url);
                    var connection = CreateHubConnection(connectionInfo.Url, connectionInfo.AccessToken);
                    var taskCompleSource = new TaskCompletionSource();
                    completionSources[user] = taskCompleSource;
                    connection.On(target, (string message) =>
                    {
                        if (message.Equals(messageToSend))
                        {
                            completionSources[user].SetResult();
                        }
                    });
                    await connection.StartAsync();
                    return connection;
                }).ToArray();
                var connections = await Task.WhenAll(tasks);

                //add connections[0] to group
                await SimpleChatClient.Group(url, new SignalRGroupAction
                {
                    ConnectionId = connections[0].ConnectionId,
                    GroupName = groupName,
                    Action = GroupAction.Add
                });
                //send messages
                await SimpleChatClient.Send(url, new SignalRMessage
                {
                    Target = target,
                    Arguments = new object[] { messageToSend },
                    GroupName = groupName
                });
                await completionSources[users[0]].Task.OrTimeout();
                Assert.False(completionSources[users[1]].Task.IsCompletedSuccessfully);

                //remove connection[0] from group and add connection[1] to group
                await SimpleChatClient.Group(url, new SignalRGroupAction
                {
                    ConnectionId = connections[0].ConnectionId,
                    GroupName = groupName,
                    Action = GroupAction.Remove
                });
                await SimpleChatClient.Group(url, new SignalRGroupAction
                {
                    ConnectionId = connections[1].ConnectionId,
                    GroupName = groupName,
                    Action = GroupAction.Add
                });
                //reset
                foreach (var user in users)
                {
                    completionSources[user] = new TaskCompletionSource();
                }
                //send messages
                await SimpleChatClient.Send(url, new SignalRMessage
                {
                    Target = target,
                    Arguments = new object[] { messageToSend },
                    GroupName = groupName
                });
                await completionSources[users[1]].Task.OrTimeout();
                Assert.False(completionSources[users[0]].Task.IsCompletedSuccessfully);
                //clean
                await Task.WhenAll(connections.Select(c => c.DisposeAsync().AsTask()));
            }
        }

        public class UserGroupManagementTest
        {
            [ConditionalTheory]
            [ClassData(typeof(BaseUrls))]
            [SkipIfSimpleChatFunctionAbsent]
            public async Task UserGroupManagement(string key, string url)
            {
                const string target = nameof(UserGroupManagementTest);
                const int count = 2;
                string groupName = Guid.NewGuid().ToString();

                var users = GenerateRandomUsers(count).ToArray();
                var messageToSend = key + Guid.NewGuid().ToString();
                var completionSources = new ConcurrentDictionary<string, TaskCompletionSource>();
                var tasks = users.Select(async user =>
                {
                    var connectionInfo = await SimpleChatClient.Negotiate(user, url);
                    var connection = CreateHubConnection(connectionInfo.Url, connectionInfo.AccessToken);
                    var taskCompleSource = new TaskCompletionSource();
                    completionSources[user] = taskCompleSource;
                    connection.On(target, (string message) =>
                    {
                        if (message.Equals(messageToSend))
                        {
                            completionSources[user].SetResult();
                        }
                    });
                    await connection.StartAsync();
                    return connection;
                }).ToArray();
                var connections = await Task.WhenAll(tasks);

                //add connections[0] to group
                await SimpleChatClient.Group(url, new SignalRGroupAction
                {
                    UserId = users[0],
                    GroupName = groupName,
                    Action = GroupAction.Add
                });
                //user group management is not a ackable task. Wait for more time to ensure it finished.
                await Task.Delay(1 * 1000);
                //send messages
                await SimpleChatClient.Send(url, new SignalRMessage
                {
                    Target = target,
                    Arguments = new object[] { messageToSend },
                    GroupName = groupName
                });
                await completionSources[users[0]].Task.OrTimeout(); ;
                Assert.False(completionSources[users[1]].Task.IsCompletedSuccessfully);

                //remove connection[0] from group and add connection[1] to group
                await SimpleChatClient.Group(url, new SignalRGroupAction
                {
                    UserId = users[0],
                    GroupName = groupName,
                    Action = GroupAction.Remove
                });
                await SimpleChatClient.Group(url, new SignalRGroupAction
                {
                    UserId = users[1],
                    GroupName = groupName,
                    Action = GroupAction.Add
                });
                //reset
                foreach (var user in users)
                {
                    completionSources[user] = new TaskCompletionSource();
                }
                await Task.Delay(1 * 1000);
                //send messages
                await SimpleChatClient.Send(url, new SignalRMessage
                {
                    Target = target,
                    Arguments = new object[] { messageToSend },
                    GroupName = groupName
                });
                await completionSources[users[1]].Task.OrTimeout(); ;
                Assert.False(completionSources[users[0]].Task.IsCompletedSuccessfully);
                //clean
                await Task.WhenAll(connections.Select(c => c.DisposeAsync().AsTask()));
            }
        }
    }
}