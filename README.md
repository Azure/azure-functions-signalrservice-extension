# Azure Functions Bindings for Azure SignalR Service

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

|Property | Attribute property |Description|
|---------|---------|----------------------|
|type|| Must be set to `signalRConnectionInfo`.|
|direction|| Must be set to `in`.|
|name|| Variable name used in function code for connection info object. |
|hubName|HubName| This value must be set to the name of the SignalR hub for which the connection information is generated.|
|userId|UserId| Optional: The value of the `UserIdentifier` claim to be set in the access key token. |
|connectionStringSetting|ConnectionStringSetting| The name of the app setting that contains the SignalR Service connection string (defaults to "AzureSignalRConnectionString"). |
|idToken|IdToken| The ID token which provides claims to be added into Azure SignalR Service token. |
|claimTypeList|ClaimTypeList| Defines the claims in `IdToken` that will be selected into Azure SignalR Service token. |

#### SignalR Output binding
The following table explains the binding configuration properties that you set in the *function.json* file and the `SignalR` attribute.

|function.json property | Attribute property |Description|
|---------|---------|----------------------|
|type|| Must be set to `signalR`.|
|direction|| Must be set to `out`.|
|name|| Variable name used in function code for connection info object. |
|hubName|HubName| This value must be set to the name of the SignalR hub for which the connection information is generated.|
|connectionStringSetting|ConnectionStringSetting| The name of the app setting that contains the SignalR Service connection string (defaults to "AzureSignalRConnectionString") |

### Current limitations

- Cannot invoke methods on a subset of connections.
- Functions cannot be triggered by client invocation of server methods (clients need to call an HTTP endpoint or post messages to a Event Grid, etc, to trigger a function)

## Usage

* [Add application settings](./docs/use-signalr-binding.md)
* [Use input binding](./docs/use-signalr-binding.md)
* [Use output binding](./docs/use-signalr-binding.md)

## Samples
* [Simple Chat Room](./samples/simple-chat)
* [Chat Room with Authentication](./samples/chat-with-auth)

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
