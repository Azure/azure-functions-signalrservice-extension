## Prerequisites

- [Azure Functions Core Tools](https://github.com/Azure/azure-functions-core-tools) (V2)

## Usage

### Create Azure SignalR Service instance

1. Create an Azure SignalR Service instance in the Azure Portal. Note the connection string, you'll need this later.

### Create Function App with extension

1. In a new folder, create a new Azure Functions app.
    - `func init`
1. Install this Functions extension.
    - `func extensions install -p Microsoft.Azure.WebJobs.Extensions.SignalRService -v 1.0.2`

### Add application settings

App settings in a function app contain global configuration options that affect all functions for that function app. For more details on how to define the app settings, please refer to [App settings reference for Azure Functions](https://docs.microsoft.com/en-us/azure/azure-functions/functions-app-settings)

An app settings sample for Azure SignalR Service Function binding could be found [here](./samples/simple-chat/csharp/FunctionApp/local.settings.sample.json).

``` json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "",
    "AzureWebJobsDashboard": "",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet",
    "AzureSignalRConnectionString": "<signalr-connection-string>",
    "AzureSignalRServiceTransportType": "Transient"
  },
  "Host": {
    "LocalHttpPort": 7071,
    "CORS": "*"
  }
}
```
Note that `AzureSignalRConnectionString` and `AzureSignalRServiceTransportType` are settings for Azure SignalR Service Function binding.

* `AzureSignalRConnectionString` is the SignalR Service's connection string, it will be used as default connection string when Function doesn't define it.
* `AzureSignalRServiceTransportType` is used for determining whether the Function establishes WebSockets connections to SignalR Service to send messages. Allowed values are `Transient` and `Persistent`, default value is `Transient`.
  * `Transient`: Send messsages by calling SignalR Service's [REST API](https://github.com/Azure/azure-signalr/blob/dev/docs/rest-api.md). This mode is appropriate when you want to send messages infrequently.
  * `Persisent`: Establish WebSockets connections for each hub, send all messages via these connections. This mode is more effecient than `Transient` mode when you want to send messages frequently or regularly.

<p align="center">
  <img src="./docs/images/transient-persistent-mode.png"/>
</p>

### Use input binding

Before a client can connect to Azure SignalR Service, it must get the service endpoint URL and a valid access token. The *SignalRConnectionInfo* input binding produces the SignalR Service endpoint URL and a valid token that are used to connect to the service. Because the token is time-limited and can be used to authenticate a specific user to a connection, you should not cache the token or share it between clients. An HTTP trigger with this binding can be used by clients to get the connection information.

See the language-specific example:

* [2.x C#](#2x-c-input-examples)
* [2.x JavaScript](#2x-javascript-input-examples)
* [2.x Java](#2x-java-input-examples)

For more information on how this binding is used to create a "negotiate" function that can be consumed by a SignalR client SDK, see the [Azure Functions development and configuration article](https://docs.microsoft.com/en-us/azure/azure-signalr/signalr-concept-serverless-development-config) in the SignalR Service concepts documentation.

#### 2.x C# input examples2.x C# input examples

The following example shows a [C# function](https://docs.microsoft.com/en-us/azure/azure-functions/functions-dotnet-class-library) that acquires SignalR connection information using the input binding and returns it over HTTP.

```cs
[FunctionName("negotiate")]
public static SignalRConnectionInfo Negotiate(
    [HttpTrigger(AuthorizationLevel.Anonymous)]HttpRequest req,
    [SignalRConnectionInfo(HubName = "chat")]SignalRConnectionInfo connectionInfo)
{
    return connectionInfo;
}
```

##### Authenticated token

If the function is triggered by an authenticated client, you can add a user ID claim to the generated token. You can easily add authentication to a function app using [App Service Authentication](https://docs.microsoft.com/en-us/azure/app-service/overview-authentication-authorization).

App Service Authentication sets HTTP headers named `x-ms-client-principal-id` and `x-ms-client-principal-name` that contain the authenticated user's client principal ID and name, respectively. You can set the `UserId` property of the binding to the value from either header using a [binding expression](https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-expressions-patterns): `{headers.x-ms-client-principal-id}` or `{headers.x-ms-client-principal-name}`. 

```cs
[FunctionName("negotiate")]
public static SignalRConnectionInfo Negotiate(
    [HttpTrigger(AuthorizationLevel.Anonymous)]HttpRequest req, 
    [SignalRConnectionInfo
        (HubName = "chat", UserId = "{headers.x-ms-client-principal-id}")]
        SignalRConnectionInfo connectionInfo)
{
    // connectionInfo contains an access key token with a name identifier claim set to the authenticated user
    return connectionInfo;
}
```

#### 2.x JavaScript input examples

The following example shows a SignalR connection info input binding in a *function.json* file and a [JavaScript function](https://docs.microsoft.com/en-us/azure/azure-functions/functions-reference-node) that uses the binding to return the connection information.

Here's binding data in the *function.json* file:

Sample of function.json:

```json
{
    "type": "signalRConnectionInfo",
    "name": "connectionInfo",
    "hubName": "chat",
    "connectionStringSetting": "<name of setting containing SignalR Service connection string>",
    "direction": "in"
}
```

Here's the JavaScript code:

```javascript
module.exports = async function (context, req, connectionInfo) {
    context.res.body = connectionInfo;
};
```

##### Authenticated token

If the function is triggered by an authenticated client, you can add a user ID claim to the generated token. You can easily add authentication to a function app using [App Service Authentication](https://docs.microsoft.com/en-us/azure/app-service/overview-authentication-authorization).

App Service Authentication sets HTTP headers named `x-ms-client-principal-id` and `x-ms-client-principal-name` that contain the authenticated user's client principal ID and name, respectively. You can set the `userId` property of the binding to the value from either header using a [binding expression](https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-expressions-patterns): `{headers.x-ms-client-principal-id}` or `{headers.x-ms-client-principal-name}`. 

Example function.json:

```json
{
    "type": "signalRConnectionInfo",
    "name": "connectionInfo",
    "hubName": "chat",
    "userId": "{headers.x-ms-client-principal-id}",
    "connectionStringSetting": "<name of setting containing SignalR Service connection string>",
    "direction": "in"
}
```

Here's the JavaScript code:

```javascript
module.exports = async function (context, req, connectionInfo) {
    // connectionInfo contains an access key token with a name identifier
    // claim set to the authenticated user
    context.res.body = connectionInfo;
};
```

#### 2.x Java input examples

The following example shows a [Java function](https://docs.microsoft.com/en-us/azure/azure-functions/functions-reference-java) that acquires SignalR connection information using the input binding and returns it over HTTP.

```java
@FunctionName("negotiate")
public SignalRConnectionInfo negotiate(
        @HttpTrigger(
            name = "req",
            methods = { HttpMethod.POST },
            authLevel = AuthorizationLevel.ANONYMOUS) HttpRequestMessage<Optional<String>> req,
        @SignalRConnectionInfoInput(
            name = "connectionInfo",
            hubName = "chat") SignalRConnectionInfo connectionInfo) {
    return connectionInfo;
}
```

##### Authenticated token

If the function is triggered by an authenticated client, you can add a user ID claim to the generated token. You can easily add authentication to a function app using [App Service Authentication](https://docs.microsoft.com/en-us/azure/app-service/overview-authentication-authorization).

App Service Authentication sets HTTP headers named `x-ms-client-principal-id` and `x-ms-client-principal-name` that contain the authenticated user's client principal ID and name, respectively. You can set the `UserId` property of the binding to the value from either header using a [binding expression](https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-expressions-patterns): `{headers.x-ms-client-principal-id}` or `{headers.x-ms-client-principal-name}`.

```java
@FunctionName("negotiate")
public SignalRConnectionInfo negotiate(
        @HttpTrigger(
            name = "req",
            methods = { HttpMethod.POST },
            authLevel = AuthorizationLevel.ANONYMOUS) HttpRequestMessage<Optional<String>> req,
        @SignalRConnectionInfoInput(
            name = "connectionInfo",
            hubName = "chat",
            userId = "{headers.x-ms-client-principal-id}") SignalRConnectionInfo connectionInfo) {
    return connectionInfo;
}
```

### Use output binding

Use the *SignalR* output binding to send one or more messages using Azure SignalR Service. You can broadcast a message to all connected clients, or you can broadcast it only to connected clients that have been authenticated to a given user.

You can also use it to manage the groups that a user belongs to.

See the language-specific example:

* [2.x C#](#2x-c-output-examples)
* [2.x JavaScript](#2x-javascript-output-examples)
* [2.x Java](#2x-java-output-examples)

#### 2.x C# output samples

##### 2.x C# send message output examples

###### Broadcast to all clients

The following example shows a [C# function](https://docs.microsoft.com/en-us/azure/azure-functions/functions-dotnet-class-library) that sends a message using the output binding to all connected clients. The `Target` is the name of the method to be invoked on each client. The `Arguments` property is an array of zero or more objects to be passed to the client method.

```cs
[FunctionName("SendMessage")]
public static Task SendMessage(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post")]object message, 
    [SignalR(HubName = "chat")]IAsyncCollector<SignalRMessage> signalRMessages)
{
    return signalRMessages.AddAsync(
        new SignalRMessage 
        {
            Target = "newMessage", 
            Arguments = new [] { message } 
        });
}
```

###### Send to a user

You can send a message only to connections that have been authenticated to a user by setting the `UserId` property of the SignalR message.

```cs
[FunctionName("SendMessage")]
public static Task SendMessage(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post")]object message, 
    [SignalR(HubName = "chat")]IAsyncCollector<SignalRMessage> signalRMessages)
{
    return signalRMessages.AddAsync(
        new SignalRMessage 
        {
            // the message will only be sent to this user ID
            UserId = "userId1",
            Target = "newMessage",
            Arguments = new [] { message }
        });
}
```

###### Send to a connection

You can send a message only to connections by setting the `ConnectionId` property of the SignalR message.

```cs
[FunctionName("SendMessage")]
public static Task SendMessage(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post")]object message, 
    [SignalR(HubName = "chat")]IAsyncCollector<SignalRMessage> signalRMessages)
{
    return signalRMessages.AddAsync(
        new SignalRMessage 
        {
            // the message will only be sent to this user ID
            ConnectionId = "connectionId",
            Target = "newMessage",
            Arguments = new [] { message }
        });
}
```

###### Send to a group

You can send a message only to clients that have been added to a group by setting the `GroupName` property of the SignalR message.

```cs
[FunctionName("SendMessage")]
public static Task SendMessage(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post")]object message,
    [SignalR(HubName = "chat")]IAsyncCollector<SignalRMessage> signalRMessages)
{
    return signalRMessages.AddAsync(
        new SignalRMessage
        {
            // the message will be sent to the group with this name
            GroupName = "myGroup",
            Target = "newMessage",
            Arguments = new [] { message }
        });
}
```

##### 2.x C# group management output examples

SignalR Service allows users to be added to groups. Messages can then be sent to a group. You can use the `SignalRGroupAction` class with the `SignalR` output binding to manage a user's group membership.

###### Add user to a group

The following example adds a user to a group.

```csharp
[FunctionName("addToGroup")]
public static Task AddToGroup(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post")]HttpRequest req,
    ClaimsPrincipal claimsPrincipal,
    [SignalR(HubName = "chat")]
        IAsyncCollector<SignalRGroupAction> signalRGroupActions)
{
    var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier);
    return signalRGroupActions.AddAsync(
        new SignalRGroupAction
        {
            UserId = userIdClaim.Value,
            GroupName = "myGroup",
            Action = GroupAction.Add
        });
}
```

###### Remove user from a group

The following example removes a user from a group.

```csharp
[FunctionName("removeFromGroup")]
public static Task RemoveFromGroup(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post")]HttpRequest req,
    ClaimsPrincipal claimsPrincipal,
    [SignalR(HubName = "chat")]
        IAsyncCollector<SignalRGroupAction> signalRGroupActions)
{
    var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier);
    return signalRGroupActions.AddAsync(
        new SignalRGroupAction
        {
            UserId = userIdClaim.Value,
            GroupName = "myGroup",
            Action = GroupAction.Remove
        });
}
```

##### Use SignalR Service Management SDK in functions

[SignalR Service Management SDK](https://www.nuget.org/packages/Microsoft.Azure.SignalR.Management) provides communication capabilities with ASP.NET Core SignalR clients through Azure SignalR Service directly. The capabilities include sending messages to all/clients/users/groups and managing group membership. We expose the `IServicemanager` and `IServiceHubContext` in `StaticServiceHubContextStore`. For how to use Management SDK, please refer to this [guide](https://github.com/Azure/azure-signalr/blob/dev/docs/management-sdk-guide.md).

SignalR input and output bindings are designed to improve user experience for Azure SignalR Service in Azure Functions. However SignalR binding is only an extrension to Azure Functions, it has some limitations, for example, it's not easy to add claims into access token in `SignalRConnectionInfo`. If you have such advanced requirements that SignalR input/output binding doesn't meet, consider using `StaticServiceHubContextStore`.

###### StaticServiceHubContextStore

A global `IServiceManagerStore` for the extension. It stores `IServiceHubContextStore` per connection string setting. For more information, see [here](./src/SignalRServiceExtension/Config/StaticServiceHubContextStore.cs).

###### IServiceHubContextStore

`IServiceHubContextStore` stores `IServiceHubContext` for each hub name.

`ValueTask<IServiceHubContext> GetAsync(string hubName)`

Gets `IServiceHubContext`. If the `IServiceHubContext` for a specific hub name exists, returns the `IServiceHubContext`, otherwise creates one and then returns it. `hubName` is the hub name of the `IServiceHubContext`. The returned value is an instance of `IServiceHubContext`.

`IServiceManager ServiceManager`

`IServiceManager` is used to create `IServiceHubContext`.

For more introduction about `IServiceHubContext` and `IServiceManager`, please refer to [Azure SignalR Service Management SDK Guide](https://github.com/Azure/azure-signalr/blob/dev/docs/management-sdk-guide.md).

> Note: We recommend you use `StaticServiceHubContextStore` instead of importing [SignalR Service Management package](https://www.nuget.org/packages/Microsoft.Azure.SignalR.Management). If you import [SignalR Service Management package](https://www.nuget.org/packages/Microsoft.Azure.SignalR.Management) directly, and you use `Persistent` [transport type](https://github.com/Azure/azure-signalr/blob/dev/docs/management-sdk-guide.md#transport-type), please don't create a `IServiceHubContext` and dispose it in a function method. Because for each http request, the function will be invoked. Therefore a new Websockets connection will be established and disconnected for each http request. That will have large impact to you function performance. Instead, you should make the `IServiceHubContext` a singleton to the function class. 

The following are samples for negotiate function without SignalR input binding and broadcast messages without output binding respectively.

###### Negotiate function without SignalR input binding

With `StaticServiceHubContextStore`, you can generate `SignalRConnectionInfo` inside the function, provide user id, hub name and additional claims extracted from http request. 

```C#
[FunctionName("negotiate")]
public static SignalRConnectionInfo GetSignalRInfo(
    [HttpTrigger(AuthorizationLevel.Anonymous)] HttpRequest req)
{
    var userId = req.Query["userid"];
    var hubName = req.Query["hubname"];
    var connectionInfo = new SignalRConnectionInfo();
    var serviceManager = StaticServiceHubContextStore.Get().ServiceManager;
    connectionInfo.AccessToken = serviceManager
        .GenerateClientAccessToken(
            hubName,
            userId,
            new List<Claim> { new Claim("claimType", "claimValue") });
    connectionInfo.Url = serviceManager.GetClientEndpoint(hubName);
    return connectionInfo;
}
```

###### Broadcast messages with output binding

`IServiceHubContext` provides [Microsoft.Azure.SignalR](https://www.nuget.org/packages/Microsoft.Azure.SignalR/)-extended interfaces, if you are familiar with how to use `IHubContext` to send messages in an app server, it will be easy for you to use `IServiceHubContext`.

``` C#
[FunctionName("broadcast")]
public static async Task Broadcast(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post")]HttpRequest req)
{
    var message = new JsonSerializer().Deserialize<ChatMessage>(new JsonTextReader(new StreamReader(req.Body)));
    var serviceHubContext = await StaticServiceHubContextStore.Get().GetAsync("simplechat");
    await serviceHubContext.Clients.All.SendAsync("newMessage", message);
}
```

#### 2.x Javascript output examples

##### 2.x Javascript send message output examples

###### Broadcast to all clients

The following example shows a [TODO function](https://docs.microsoft.com/en-us/azure/azure-functions/functions-dotnet-class-library) that sends a message using the output binding to all connected clients. The `Target` is the name of the method to be invoked on each client. The `Arguments` property is an array of zero or more objects to be passed to the client method.

###### Send to a user

You can send a message only to connections that have been authenticated to a user by setting the `UserId`[todo] property of the SignalR message.

###### Send to a connection

You can send a message only to connections by setting the `ConnectionId`[todo] property of the SignalR message.

###### Send to a group

You can send a message only to clients that have been added to a group by setting the `GroupName`[todo] property of the SignalR message.

##### 2.x [todo] group management output examples

SignalR Service allows users to be added to groups. Messages can then be sent to a group. You can use the `SignalRGroupAction` class with the `SignalR` output binding to manage a user's group membership.

###### Add user to a group

The following example adds a user to a group.

###### Remove user from a group

The following example removes a user from a group.

More samples can be found [here](./samples/).

#### 2.x Java output examples

##### 2.x Java send message output examples

###### Broadcast to all clients

The following example shows a [TODO function](https://docs.microsoft.com/en-us/azure/azure-functions/functions-dotnet-class-library) that sends a message using the output binding to all connected clients. The `Target` is the name of the method to be invoked on each client. The `Arguments` property is an array of zero or more objects to be passed to the client method.

###### Send to a user

You can send a message only to connections that have been authenticated to a user by setting the `UserId`[todo] property of the SignalR message.

###### Send to a connection

You can send a message only to connections by setting the `ConnectionId`[todo] property of the SignalR message.

###### Send to a group

You can send a message only to clients that have been added to a group by setting the `GroupName`[todo] property of the SignalR message.

##### 2.x [todo] group management output examples

SignalR Service allows users to be added to groups. Messages can then be sent to a group. You can use the `SignalRGroupAction` class with the `SignalR` output binding to manage a user's group membership.

###### Add user to a group

The following example adds a user to a group.

###### Remove user from a group

The following example removes a user from a group.

More samples can be found [here](./samples/).