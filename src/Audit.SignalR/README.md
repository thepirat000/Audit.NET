# Audit.SignalR

**SignalR Auditing Extension Module for [Audit.NET library](https://github.com/thepirat000/Audit.NET).**

Automatically generates Audit Logs for SignalR invokations.

Audit.SignalR provides a configurable [Hub Pipeline Module](https://docs.microsoft.com/en-us/aspnet/signalr/overview/guide-to-the-api/hubs-api-guide-server#hubpipeline) 
that intercepts the hub processing to generate an audit trail.

It generate logs for the following events:

| Event Name | Description | 
| ------------ |  -------------- |
| Connect | Client is connecting to the server |
| Reconnect | Client reconnecting to the server |
| Disconnect | Client disconnect from the server |
| Outgoing | Server invoking client-side method |
| Incoming | Client invoking server-side method |
| Error | An error has occurred |

## Install

**NuGet Package** 

To install the package run the following command on the Package Manager Console:

```
PM> Install-Package Audit.SignalR
```

[![NuGet Status](https://img.shields.io/nuget/v/Audit.SignalR.svg?style=flat)](https://www.nuget.org/packages/Audit.SignalR/)
[![NuGet Count](https://img.shields.io/nuget/dt/Audit.SignalR.svg)](https://www.nuget.org/packages/Audit.SignalR/)

## Note 

This library is **not** compatible with ASP.NET **Core**. This is because the ASP.NET Core does not includes the hub pipeline.
Check this [issue](https://github.com/aspnet/SignalR/issues/924).


## Usage
On your ASP.NET startup logic, call the extension method `AddAuditModule()` defined on namespace `Microsoft.AspNet.SignalR.GlobalHost.HubPipeline`, to setup the audit pipeline. This must be called before `IAppBuilder.MapSignalR()`.

This method provides a fluent API to configure the audit module.

For example:
```c#
using Audit.SignalR;
using Microsoft.AspNet.SignalR;
//...
public class Startup
{
    public void Configuration(IAppBuilder app)
    {
        GlobalHost.HubPipeline.AddAuditModule(config => config
            .IncludeHeaders()
            .Filters(f => f
                .IncludeOutgoingEvent(og => og.HubName == "myHub")
                .IncludeReconnectEvent(false));
        app.MapSignalR();
    }
}
```

This will create an `AuditPipelineModule` (that inherits from [HubPipelineModule](https://msdn.microsoft.com/en-us/library/microsoft.aspnet.signalr.hubs.hubpipelinemodule(v=vs.118).aspx))
and will add it to the hub pipeline.

Alternatively, you can create an instance of the module and add it to the pipeline, with the exact same effect:

```c#
using Audit.SignalR;
using Microsoft.AspNet.SignalR;
//...
var module = new AuditPipelineModule()
{
    IncludeHeaders = true,
    ReconnectEventsFilter = _ => false,
    OutgoingEventsFilter = og => og.HubName == "myHub"
};
GlobalHost.HubPipeline.AddModule(module);
```

Or create the module using the factory method `AuditPipelineModule.Create()` that provides the fluent API for the configuration:

```c#
var module = AuditPipelineModule.Create(config => config
    .IncludeHeaders()
    ...
);
GlobalHost.HubPipeline.AddModule(module);
```

## Configuration

### Settings
The following settings can be configured on the module:

- **AuditEventType**: To indicate the event type to use on the audit event. (Default is the event name). Can contain the following placeholders: 
   - \{event}: replaced with the SignalR event name (Connect, Reconnect, Disconnect, Incoming, Outgoing, Error).
- **IncludeHeaders**: To indicate if the audit should include the request headers (Valid for events Connect, Reconnect, Disconnect, Incoming and Error). Default is false.
- **IncludeQueryString**: To indicate if the audit should include the request Query String (Valid for events Connect, Reconnect, Disconnect, Incoming and Error). Default is false.
- **AuditDisabled**: Set to true to disable the audit log generation (bypass). Default is false.
- **CreationPolicy**: To indicate the event creation policy to use. Default is NULL to use the [globally configured creation policy](https://github.com/thepirat000/Audit.NET#creation-policy).
- **AuditDataProvider**: To indicate the Audit Data Provider to use. Default is NULL to use the [globally configured data provider](https://github.com/thepirat000/Audit.NET#data-provider).

#### Filter settings
The module allows to configure filtering of events. By default it will log all the events.
- **IncomingEventsFilter**: Allows to filter Incoming events (invoking server-side methods) with a custom function that given the incoming event info, returns true if the event should be logged and false otherwise.
- **OutgoingEventsFilter**: Allows to filter Outgoing events (invoking client-side methods).
- **ConnectEventsFilter**: Allows to filter Connect events.
- **DisconnectEventsFilter**: Allows to filter Disconnect events.
- **ReconnectEventsFilter**: Allows to filter Reconnect events.
- **ErrorEventsFilter**: Allows to filter Error events.

### Extendion Methods

You can access the current [Audit Scope](https://github.com/thepirat000/Audit.NET#usage) 
for the incoming event inside server-side methods, by calling the Hub extension method `GetIncomingAuditScope()`, 
for example:

```c#
public class MyHub : Hub
{
    public int Send(string name, string message)
    {
        AuditScope scope = this.GetIncomingAuditScope();
        if (someCondition)
        {
            scope.Discard()
        }
        scope.SetCustomField("UserType", user.UserType);

        //...
    }
```

The SignalR specific event information on the AuditScope can be accessed as follows:

```c#
AuditScope scope = this.GetIncomingAuditScope();
var auditEvent = scope.Event as AuditEventSignalr;
var signalrEventIncoming = auditEvent.Event as SignalrEventIncoming;
```

Alternatively you can use the `GetSignalrEvent()` extension method provided:
```c#
AuditScope scope = this.GetIncomingAuditScope();
var signalrEventIncoming = scope.Event.GetSignalrEvent<SignalrEventIncoming>();
```

### Event Output 

To configure the output persistence mechanism please see [Configuration](https://github.com/thepirat000/Audit.NET#configuration) and [Data Providers](https://github.com/thepirat000/Audit.NET#data-providers) sections.

## Output

Audit.SignalR output includes:

- Execution time and duration (when applicable)
- Environment information such as user, machine, domain and locale.
- ConnectionId
- Hub name, Method name and Arguments
- Exception details
- HTTP request details (optional)

## Output details

The following tables describes the Audit.SignalR output fields per event type:

- ### [SignalrEventConnect](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.SignalR/SignalrEventConnect.cs)
| Field Name | Type | Description | 
| ------------ | ---------------- |  -------------- |
| **EventType** | SignalrEventType | "Connect" |
| **ConnectionId** | string | The connection ID |
| **Headers** | Dictionary<string, string> | The HTTP headers of the associated request (when IncludeHeaders is set to true) |
| **QueryString** | Dictionary<string, string> | The query string of the associated request (when IncludeQueryString is set to true) |
| **LocalPath** | string | The local path  |
| **IdentityName** | string | The identity name associated |

- ### [SignalrEventDisconnect](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.SignalR/SignalrEventDisconnect.cs)
| Field Name | Type | Description | 
| ------------ | ---------------- |  -------------- |
| **EventType** | SignalrEventType | "Disconnect" |
| **ConnectionId** | string | The connection ID |
| **Headers** | Dictionary<string, string> | The HTTP headers of the associated request (when IncludeHeaders is set to true) |
| **QueryString** | Dictionary<string, string> | The query string of the associated request (when IncludeQueryString is set to true) |
| **LocalPath** | string | The local path  |
| **IdentityName** | string | The identity name associated |
| **StopCalled** | boolean | Set to true if the client explicitly closed the connection |

- ### [SignalrEventIncoming](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.SignalR/SignalrEventIncoming.cs)
| Field Name | Type | Description | 
| ------------ | ---------------- |  -------------- |
| **EventType** | SignalrEventType | "Incoming" |
| **ConnectionId** | string | The connection ID |
| **Headers** | Dictionary<string, string> | The HTTP headers of the associated request (when IncludeHeaders is set to true) |
| **QueryString** | Dictionary<string, string> | The query string of the associated request (when IncludeQueryString is set to true) |
| **LocalPath** | string | The local path  |
| **IdentityName** | string | The identity name associated |
| **HubName** | string | The hub name |
| **HubType** | string | The hub type name |
| **MethodName** | string | The name of the invoked method |
| **Args** | List<Object> | The arguments passed in the invokation |
| **Result** | Object | The invoked method return value |

- ### [SignalrEventOutgoing](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.SignalR/SignalrEventOutgoing.cs)
| Field Name | Type | Description | 
| ------------ | ---------------- |  -------------- |
| **EventType** | SignalrEventType | "Outgoing" |
| **HubName** | string | The hub name |
| **Signal** | string | The signal (ConnectionId, hub type name or hub type name + "." + group name) belonging to clients that receive the method invocation |
| **MethodName** | string | The invoked method name |
| **Args** | List<Object> | The arguments passed in the invokation |

- ### [SignalrEventError](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.SignalR/SignalrEventError.cs) : SignalrEventConnect
| Field Name | Type | Description | 
| ------------ | ---------------- |  -------------- |
| **EventType** | SignalrEventType | "Error" |
| **ConnectionId** | string | The connection ID |
| **Headers** | Dictionary<string, string> | The HTTP headers of the associated request (when IncludeHeaders is set to true) |
| **QueryString** | Dictionary<string, string> | The query string of the associated request (when IncludeQueryString is set to true) |
| **LocalPath** | string | The local path  |
| **IdentityName** | string | The identity name associated |
| **HubName** | string | The hub name |
| **HubType** | string | The hub type name |
| **Exception** | string | Detailed information about the exception |
| **MethodName** | string | The name of the invoked method |
| **Args** | List<Object> | The arguments passed in the invokation |

## Customization

### Custom fields

You can add extra information as [custom fields](https://github.com/thepirat000/Audit.NET#custom-fields-and-comments) to the events by calling the method `AddAuditCustomField` on your `Hub`. For example:

```c#
public class MyHub : Hub
{
    public int Send(string name, string message)
    {
        this.AddCustomField("User", user);
    }
```

Another way to customize the output is by using global custom actions, please see [custom actions](https://github.com/thepirat000/Audit.NET#custom-actions) for more information.
