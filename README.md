# Azure Functions bindings for Azure SignalR Service

## Intro

These bindings allow Azure Functions to integrate with Azure SignalR Service.

### Supported scenarios

- Allow clients to serverlessly connect to a SignalR Service hub without requiring an ASP.NET Core backend
- Use Azure Functions (any language supported by V2) to broadcast messages to all clients connected to a SignalR Service hub
- Example scenarios include: broadcast messages to a SignalR Service hub on HTTP requests and events from Cosmos DB change feed, Event Hub, Event Grid, etc

### Bindings

`SignalRConnectionInfo` input binding makes it easy to generate the token required for clients to initiate a connection to Azure SignalR Service.

`SignalR` output binding allows messages to be broadcast to an Azure SignalR Service hub.

### Current limitations

- Only supports broadcasting at this time, cannot invoke methods on a subset of connections, users, or groups
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
    - `func extensions install -p AzureAdvocates.WebJobs.Extensions.SignalRService -v 0.2.0-alpha`

### Add application setting for SignalR connection string

1. Create an app setting called `AzureSignalRConnectionString` with the SignalR connection string.
    - On localhost, use `local.settings.json`
    - In Azure, use App Settings

### Using the SignalRConnectionInfo input binding

In order for a client to connect to SignalR, it needs to obtain the SignalR Service client hub URL and an access token.

1. Create a new function named `negotiate` and use the `SignalRConnectionInfo` input binding to obtain the connection information and return it. Take a look at this [sample](samples/simple-chat/functionapp/negotiate/).
1. Before connecting to the SignalR Service, the client needs to call this function to obtain the endpoint URL and access token. See [this file](samples/simple-chat/content/index.html) for a sample usage.

Binding schema:

```javascript
{
  "type": "signalRConnectionInfo",
  "name": "connectionInfo",
  "hubName": "<hub_name>",
  "connectionStringSetting": "<setting_name>", // Defaults to AzureSignalRConnectionString
  "direction": "in"
}
```

### Using the SignalR output binding

The `SignalR` output binding can be used to broadcast messages to all clients connected a hub. Take a look at this sample:

- [HttpTrigger function to send messages](samples/simple-chat/functionapp/messages/)
- [Simple chat app](samples/simple-chat/content/index.html)
    - Calls negotiate endpoint to fetch connection information
    - Connects to SignalR Service
    - Sends messages to HttpTrigger function, which then broadcasts the messages to all clients

Binding schema:

```javascript
{
  "type": "signalR",
  "name": "signalRMessages", // name of the output binding
  "hubName": "<hub_name>",
  "connectionStringSetting": "<setting_name>", // Defaults to AzureSignalRConnectionString
  "direction": "out"
}
```

To send one or more messages, set the output binding to an array of objects:

```javascript
module.exports = function (context, req) {
  context.bindings.signalRMessages = [{
    "target": "newMessage", // name of the client method to invoke
    "arguments": [
      req.body // arguments to pass to client method
    ]
  }];
  context.done();
};
```
