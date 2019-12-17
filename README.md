# Azure Functions Bindings for Azure SignalR Service  <!-- omit in toc -->

## Content  <!-- omit in toc -->

- [Build Status](#build-status)
- [NuGet Packages](#nuget-packages)
- [Intro](#intro)
  - [Supported scenarios](#supported-scenarios)
  - [Bindings](#bindings)
  - [Current limitations](#current-limitations)
- [Prerequisites](#prerequisites)
- [Usage](#usage)
  - [Create Azure SignalR Service instance](#create-azure-signalr-service-instance)
  - [Create Function App with extension](#create-function-app-with-extension)
  - [Add application setting for SignalR connection string](#add-application-setting-for-signalr-connection-string)
  - [Using the SignalRConnectionInfo input binding](#using-the-signalrconnectioninfo-input-binding)
  - [2.x C# input examples](#2x-c-input-examples)
  - [2.x JavaScript input examples](#2x-javascript-input-examples)
  - [2.x Java input examples](#2x-java-input-examples)
- [SignalR output binding](#signalr-output-binding)
  - [2.x C# send message output examples](#2x-c-send-message-output-examples)
  - [2.x C# group management output examples](#2x-c-group-management-output-examples)
- [Advanced Topics](#advanced-topics)
- [Contributing](#contributing)

## Build Status

Travis: [![travis](https://travis-ci.org/Azure/azure-functions-signalrservice-extension.svg?branch=dev)](https://travis-ci.org/Azure/azure-functions-signalrservice-extension)

## NuGet Packages

Package Name | Target Framework | NuGet
---|---|---
Microsoft.Azure.WebJobs.Extensions.SignalRService | .NET Standard 2.0 | [![NuGet](https://img.shields.io/nuget/v/Microsoft.Azure.WebJobs.Extensions.SignalRService.svg)](https://www.nuget.org/packages/Microsoft.Azure.WebJobs.Extensions.SignalRService)

## Intro

These bindings allow Azure Functions to integrate with [Azure SignalR Service](http://aka.ms/signalr_service).

### Supported scenarios

- Allow clients to serverlessly connect to a SignalR Service hub without requiring an ASP.NET Core backend
- Use Azure Functions (any language supported by V2) to broadcast messages to all clients connected to a SignalR Service hub.
- Use Azure Functions (any language supported by V2) to send messages to a single user, or all the users in a group.
- Use Azure Functions (any language supported by V2) to manage group users like add/remove a single user in a group.
- Use Azure Functions (any language supported by V2) to remove a user from all groups.
- Example scenarios include: broadcast messages to a SignalR Service hub on HTTP requests and events from Cosmos DB change feed, Event Hub, Event Grid, etc

### Bindings

`SignalRConnectionInfo` input binding makes it easy to generate the token required for clients to initiate a connection to Azure SignalR Service.

`SignalR` output binding allows messages to be broadcast to an Azure SignalR Service hub.

#### SignalRConnectionInfo Input Binding

The following table explains the binding configuration properties that you set in the *function.json* file and the `SignalRConnectionInfo` attribute.

|function.json property | Attribute property |Description|
|---------|---------|----------------------|
|**type**|| Must be set to `signalRConnectionInfo`.|
|**direction**|| Must be set to `in`.|
|**name**|| Variable name used in function code for connection info object. |
|**hubName**|**HubName**| This value must be set to the name of the SignalR hub for which the connection information is generated.|
|**userId**|**UserId**| Optional: The value of the user identifier claim to be set in the access key token. |
|**connectionStringSetting**|**ConnectionStringSetting**| The name of the app setting that contains the SignalR Service connection string (defaults to "AzureSignalRConnectionString"). |
|**idToken**|**IdToken**| The ID token which provide claims to be added into Azure SignalR Service token. |
|**claimTypeList**|**ClaimTypeList**| Defines the claims in `IdToken` that will be selected into Azure SignalR Service token. |

#### SignalR Output binding
The following table explains the binding configuration properties that you set in the *function.json* file and the `SignalR` attribute.

|function.json property | Attribute property |Description|
|---------|---------|----------------------|
|**type**|| Must be set to `signalR`.|
|**direction**|| Must be set to `out`.|
|**name**|| Variable name used in function code for connection info object. |
|**hubName**|**HubName**| This value must be set to the name of the SignalR hub for which the connection information is generated.|
|**connectionStringSetting**|**ConnectionStringSetting**| The name of the app setting that contains the SignalR Service connection string (defaults to "AzureSignalRConnectionString") |

### Current limitations

- Cannot invoke methods on a subset of connections.
- Functions cannot be triggered by client invocation of server methods (clients need to call an HTTP endpoint or post messages to a Event Grid, etc, to trigger a function)

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

### Add application setting for SignalR connection string

1. Create an app setting called `AzureSignalRConnectionString` with the SignalR connection string.
    - On localhost, use `local.settings.json`
    - In Azure, use App Settings

### Using the SignalRConnectionInfo input binding

Before a client can connect to Azure SignalR Service, it must retrieve the service endpoint URL and a valid access token. The *SignalRConnectionInfo* input binding produces the SignalR Service endpoint URL and a valid token that are used to connect to the service. Because the token is time-limited and can be used to authenticate a specific user to a connection, you should not cache the token or share it between clients. An HTTP trigger using this binding can be used by clients to retrieve the connection information.

See the language-specific example:

* [2.x C#](#2x-c-input-examples)
* [2.x JavaScript](#2x-javascript-input-examples)
* [2.x Java](#2x-java-input-examples)

For more information on how this binding is used to create a "negotiate" function that can be consumed by a SignalR client SDK, see the [Azure Functions development and configuration article](https://docs.microsoft.com/en-us/azure/azure-signalr/signalr-concept-serverless-development-config) in the SignalR Service concepts documentation.

### 2.x C# input examples

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

#### Authenticated tokens

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

### 2.x JavaScript input examples

The following example shows a SignalR connection info input binding in a *function.json* file and a [JavaScript function](https://docs.microsoft.com/en-us/azure/azure-functions/functions-reference-node) that uses the binding to return the connection information.

Here's binding data in the *function.json* file:

Example function.json:

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

#### Authenticated tokens

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

### 2.x Java input examples

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

#### Authenticated tokens

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

## SignalR output binding

Use the *SignalR* output binding to send one or more messages using Azure SignalR Service. You can broadcast a message to all connected clients, or you can broadcast it only to connected clients that have been authenticated to a given user.

You can also use it to manage the groups that a user belongs to.

See the language-specific example:

* [2.x C#](#2x-c-send-message-output-examples)
* [2.x JavaScript](#2x-javascript-send-message-output-examples)
* [2.x Java](#2x-java-send-message-output-examples)

### 2.x C# send message output examples

#### Broadcast to all clients

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

#### Send to a user

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

#### Send to a group

You can send a message only to connections that have been added to a group by setting the `GroupName` property of the SignalR message.

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

### 2.x C# group management output examples

SignalR Service allows users to be added to groups. Messages can then be sent to a group. You can use the `SignalRGroupAction` class with the `SignalR` output binding to manage a user's group membership.

#### Add user to a group

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

#### Remove user from a group

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

The full samples can be found [here](./samples/).

#### Use SignalR Service Management SDK in functions

[SignalR Service Management SDK](https://www.nuget.org/packages/Microsoft.Azure.SignalR.Management) provides communication capabilities with ASP.NET Core SignalR clients through Azure SignalR Service directly. The capabilities include sending messages to all/clients/users/groups and managing group membership. We expose the `IServicemanager` and `IServiceHubContext` in `StaticServiceHubContextStore`. For how to use Management SDK, please refer to this [guide](https://github.com/Azure/azure-signalr/blob/dev/docs/management-sdk-guide.md).

SignalR input and output bindings are designed to improve user experience for Azure SignalR Service in Azure Functions. However SignalR binding is only an extrension to Azure Functions, it has some limitations, for example, you don't have an easy way to add claims into access token in `SignalRConnectionInfo`. If you have such advanced requirements that SignalR input/output binding doesn't meet, consider using `StaticServiceHubContextStore`.

##### StaticServiceHubContextStore

A global `IServiceManagerStore` for the extension. It stores `IServiceHubContextStore` per connection string setting.

`public static IServiceHubContextStore Get(string configurationKey = Constants.AzureSignalRConnectionStringName)`
Get `IServiceHubContextStore` for `configurationKey`. The default `configurationKey` is "AzureSignalRConnectionString".

##### IServiceHubContextStore

`IServiceHubContextStore` stores `IServiceHubContext` for each hub name.

`ValueTask<IServiceHubContext> GetAsync(string hubName)`

Gets `IServiceHubContext`. If the `IServiceHubContext` for a specific hub name exists, returns the `IServiceHubContext`, otherwise creates one and then returns it. `hubName` is the hub name of the `IServiceHubContext`. The returned value is an instance of `IServiceHubContext`.

`IServiceManager ServiceManager`

`IServiceManager` is used to create `IServiceHubContext`.

For more introduction about `IServiceHubContext` and `IServiceManager`, please refer to [Azure SignalR Service Management SDK Guide](https://github.com/Azure/azure-signalr/blob/dev/docs/management-sdk-guide.md).

> Note: We recommend you use `StaticServiceHubContextStore` instead of importing [SignalR Service Management package](https://www.nuget.org/packages/Microsoft.Azure.SignalR.Management). If you import [SignalR Service Management package](https://www.nuget.org/packages/Microsoft.Azure.SignalR.Management) directly, and you use `Persistent` [transport type](https://github.com/Azure/azure-signalr/blob/dev/docs/management-sdk-guide.md#transport-type), please don't create a `IServiceHubContext` and dispose it in a function method. Because for each http request, the function will be invoked. Therefore a new Websockets connection will be established and disconnected for each http request. That will have large impact to you function performance. Instead, you should make the `IServiceHubContext` a singleton to the function class. 

The following are sample for negotiate function without SignalR input binding and broadcast messages without output binding respectively.

##### Negotiate function without SignalR input binding

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

##### Broadcast messages with output binding

`IServiceHubContext` provide [Microsoft.Azure.SignalR](https://www.nuget.org/packages/Microsoft.Azure.SignalR/)-extended interfaces, if you are familiar how to use `IHubContext` to send messages in an app server, it will be easy for you to use `IServiceHubContext`.

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

## Advanced Topics
* [Group Management](https://github.com/Azure/azure-signalr/blob/dev/docs/group-management.md)

## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.microsoft.com.

When you submit a pull request, a CLA-bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., label, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
