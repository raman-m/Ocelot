using Microsoft.ServiceFabric.Services.Runtime;
using Ocelot.Samples.ServiceFabric.DownstreamService;

try
{
    // The ServiceManifest.XML file defines one or more service type names.
    // Registering a service maps a service type name to a .NET type.
    // When Service Fabric creates an instance of this service type,
    // an instance of the class is created in this host process.
    await ServiceRuntime.RegisterServiceAsync("OcelotApplicationServiceType",
        (context) => new ApiGateway(context));

    ServiceEventSource.Current.ServiceTypeRegistered(Environment.ProcessId, nameof(ApiGateway));

    // Prevents this host process from terminating so services keeps running. 
    Thread.Sleep(Timeout.Infinite);
}
catch (Exception e)
{
    ServiceEventSource.Current.ServiceHostInitializationFailed(e.ToString());
    throw;
}
