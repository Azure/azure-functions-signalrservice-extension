# Multiple Azure SignalR Service Instances Support
Currently we support multiple Azure SignalR Service instances **for Persistent mode only**.

## Set up multiple endpoints
To enable multiple endpoints support, you should: 

1. Set the `ServiceTransportType` to `Persistent` in your configuration.
```json
{
    "AzureSignalRServiceTransportType":"Persistent"
}
```

2. Set multiple SignalR endpoints entries in your configuration. The keys are in the following format : 
```
Azure:SignalR:Endpoints: <name> : <role>
```

The followings are some service endpoint entry samples.
```json
{
    
    "Azure:SignalR:Endpoints:EastUs":"<value>",
    
    "Azure:SignalR:Endpoints:EastUs2:Secondary":"<value>",
    
    "Azure:SignalR:Endpoints:WestUs:Primary":"<value>"
}
```

**When you configure Azure SignalR endpoints in the App Service on Azure portal,  don't forget to replace `":"` with `"__"`, the double underscore in the keys.** For reasons, see [Environment variables](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-5.0#environment-variables).


## Configuration in cross-geo scenarios
 For details about `<role>` , see [Configuration in cross-geo scenarios](https://github.com/Azure/azure-signalr/blob/dev/docs/sharding.md#configuration-in-cross-geo-scenarios).


<!--Todo How to customize router -->

<!--Todo New methods for class-based mode-->