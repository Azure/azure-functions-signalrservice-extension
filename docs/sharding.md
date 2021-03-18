# Multiple Azure SignalR Service instances Support
Currently we add support for configuring multiple SignalR Service instances **under Persistent mode**. You can distrubute your clients to multiple SignalR service instances and send messages to multiple instances as if to one instance. 

<!-- TOC -->

- [Multiple Azure SignalR Service instances Support](#multiple-azure-signalr-service-instances-support)
  - [Usage Scenarios](#usage-scenarios)
  - [Configuration method](#configuration-method)
  - [Route algorithm](#route-algorithm)
    - [Default behavior](#default-behavior)
    - [Customization](#customization)
    - [Implicitly pass HttpContext to endpoint router](#implicitly-pass-httpcontext-to-endpoint-router)

<!-- /TOC -->

## Usage Scenarios
Routing logic is the way to decide to which SignalR Service instance among multiple instances your clients connect and your messages send. By applying different routing logic, this feature can be used in different scenarios. 
* Scaling. Randomly route each client to one SignalR Service instance, send messages to all the SignalR Service instances so that you can scale the concurrent connections.
* Cross-geo scenario. Cross-gen networks can be comparatively unstable. Route your clients to a SignalR Service instance in the same region can reduce cross-gen connections.
* High availability and disaster recovery scenarios. Set up multiple service instances in different regions, so when one region is down, the others can be used as backup. Configure service instances as two roles, **primary** and **secondary**. By default, clients will be routed to a primary online instance. When SDK detects all the primary instances are down, it will route clients to secondary instances.

## Configuration method

To enable multiple SignalR Service instances, you should: 

1. Use `Persistent` transport type.

    Currently multiple-endpoint feature is only support on `Persistent` mode. Add the followicng entry to your `local.settings.json` file locally or the application setting on Azure.

    ```json
    {
        "AzureSignalRServiceTransportType":"Persistent"
    }
    ```

2. Configure multiple SignalR Service endpoints entries in your configuration.

    We use [`ServiceEndpoint`](https://github.com/Azure/azure-signalr/blob/dev/src/Microsoft.Azure.SignalR.Common/Endpoints/ServiceEndpoint.cs) object to represent a SignalR Service instance. You can define an service endpoint with its `<Name>` and `<EndpointType>` in the entry key, and the connection string in the entry value. The keys are in the following format : 

    ```
    Azure:SignalR:Endpoints:<Name>:<EndpointType>
    ```

    `<EndpointType>` is optional and is `primary` by default. The followings are some service endpoint entry samples.
        
    ```json
    {
        "Azure:SignalR:Endpoints:EastUs":"<ConnectionString>",
        
        "Azure:SignalR:Endpoints:EastUs2:Secondary":"<ConnectionString>",
        
        "Azure:SignalR:Endpoints:WestUs:Primary":"<ConnectionString>"
    }
    ```

    > When you configure Azure SignalR endpoints in the App Service on Azure portal, don't forget to replace `":"` with `"__"`, the double underscore in the keys. For reasons, see [Environment variables](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-5.0#environment-variables).

## Route algorithm

### Default behavior
By default, the SDK uses the [DefaultEndpointRouter](https://github.com/Azure/azure-signalr/blob/dev/src/Microsoft.Azure.SignalR/EndpointRouters/DefaultEndpointRouter.cs) to pick up endpoints.

* Client routing: Randomly select one endpoint from **primary online** endpoints. If all the primary endpoints are offline, then randomly select one **secondary online** endpoint. If the selection fails again, then exception is thrown.

* Server message routing: All service endpoints are returned.

### Customization
We support cutomization of route algoritm in C# language. 

Here are the steps:
* Implement a cutomized router. You can leverage information provided from [`ServiceEndpoint`](https://github.com/Azure/azure-signalr/blob/dev/src/Microsoft.Azure.SignalR.Common/Endpoints/ServiceEndpoint.cs) to make routing decision. See guide here: [customize-route-algorithm](https://github.com/Azure/azure-signalr/blob/dev/docs/sharding.md#customize-route-algorithm).

* Register the router to DI container.
```cs
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.SignalR;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(SimpleChatV3.Startup))]
namespace SimpleChatV3
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddSingleton<IEndpointRouter, CustomizedRouter>();
        }
    }
}
```

For other languages such as Javascript, we will support route algorithm customization in the future.

### Implicitly pass HttpContext to endpoint router
If your endpoint router needs `HttpContext` to make decision, you should use an [Http trigger](https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-http-webhook-trigger?tabs=csharp) along with `SignalRConnectionInfo` input binding, so that the `HttpContext` inside the `HttpRequest` will be passed to the router.

For examle:
```cs
[FunctionName("negotiate")]
public static SignalRConnectionInfo GetSignalRInfo(
    [HttpTrigger(AuthorizationLevel.Anonymous)] HttpRequest req,
    [SignalRConnectionInfo(HubName = "simplechat", UserId = "{query.userid}")] SignalRConnectionInfo connectionInfo)
{
    return connectionInfo;
}
```
<!--Todo New methods for class-based mode-->