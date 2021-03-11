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

2. Set multiple SignalR connection string entries in your configuration. The keys are in the following format : 
```
AzureSignalRConnectionString: <name> : <role>
```
You can customize the key prefix `AzureSignalRConnectionString` with `ConnectionStringSetting`. `<name>` is the name of the endpoint and `<role>` is its role (`primary` or `secondary`). Name is optional but it will be useful if you want to further customize the routing behavior among multiple endpoints. If `<role>` is not specified, it will default to `primary`.

The followings are some connection string entries samples.
```json
{
    "AzureSignalRConnectionString":"<value>",
    
    "AzureSignalRConnectionString:EastUs":"<value>",
    
    "AzureSignalRConnectionString:EastUs2:Secondary":"<value>",
    
    "AzureSignalRConnectionString:WestUs:Primary":"<value>"
}
```

**When you configure connection strings in the App Service on Azure portal,  don't forget to replace `":"` with `"__"`, the double underscore**. For reasons, see [Environment variables](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-5.0#environment-variables).


## Configuration in cross-geo scenarios
 For details about `<role>` , see [Configuration in cross-geo scenarios](https://github.com/Azure/azure-signalr/blob/dev/docs/sharding.md#configuration-in-cross-geo-scenarios).


<!--Todo How to customize router -->

<!--Todo New methods for class-based mode-->