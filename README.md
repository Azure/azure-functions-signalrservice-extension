# Azure Functions bindings for SignalR Service

## Prerequisites

- [Azure Functions Core Tools](https://github.com/Azure/azure-functions-core-tools) (V2)

## Usage

### Create Azure SignalR Service instance

1. Create an Azure SignalR Service instance in the Azure Portal. Note the connection string, you'll need this later.

### Create Function App with extension

1. In a new folder, create a new Azure Functions app.
    - `func init`
1. Install this Functions extension.
    - `func extensions install -p AzureAdvocates.WebJobs.Extensions.SignalRService -v 0.1.0-alpha`

### Add application setting for SignalR connection string

1. Create an app setting called `AzureSignalRConnectionString` with the SignalR connection string.
    - On localhost, use `local.settings.json`
    - In Azure, use App Settings

### Using the SignalRToken input binding

In order for a client to connect to SignalR, it needs to obtain the SignalR Service client hub URL and an access token.

1. Create a new function named `negotiate` and use the `SignalRToken` input binding to obtain the connection information and return it. Take a look at this [sample](samples/simple-chat/functionapp/negotiate/).

### Using the SignalR output binding

The `SignalR` output binding can be used to broadcast messages to all clients connected a hub. Take a look at this sample:

- [HttpTrigger function to send messages](samples/simple-chat/functionapp/messages/)
- [Simple chat app](samples/simple-chat/content/index.html)
    - Calls negotiate endpoint to fetch connection information
    - Connects to SignalR Service
    - Send messages to HttpTrigger function, which in-turn broadcasts the messages back


