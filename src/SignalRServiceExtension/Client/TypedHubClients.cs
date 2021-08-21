// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.SignalR;
using System.Collections.Generic;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
  internal class TypedHubClients<T> : IHubClients<T>
  {
    private readonly IHubClients _hubClients;

    public TypedHubClients(IHubClients dynamicContext)
    {
      _hubClients = dynamicContext;
    }

    public T All => TypedClientBuilder<T>.Build(_hubClients.All);

    public T AllExcept(IReadOnlyList<string> excludedConnectionIds) => TypedClientBuilder<T>.Build(_hubClients.AllExcept(excludedConnectionIds));

    public T Client(string connectionId) => TypedClientBuilder<T>.Build(_hubClients.Client(connectionId));

    public T Group(string groupName) => TypedClientBuilder<T>.Build(_hubClients.Group(groupName));

    public T GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => TypedClientBuilder<T>.Build(_hubClients.GroupExcept(groupName, excludedConnectionIds));

    public T Clients(IReadOnlyList<string> connectionIds) =>  TypedClientBuilder<T>.Build(_hubClients.Clients(connectionIds));

    public T Groups(IReadOnlyList<string> groupNames) => TypedClientBuilder<T>.Build(_hubClients.Groups(groupNames));

    public T User(string userId) => TypedClientBuilder<T>.Build(_hubClients.User(userId));

    public T Users(IReadOnlyList<string> userIds) => TypedClientBuilder<T>.Build(_hubClients.Users(userIds));
  }
}
