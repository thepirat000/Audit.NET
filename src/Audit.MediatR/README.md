# Audit.MediatR

MediatR auditing behavior for the [Audit.NET](https://github.com/thepirat000/Audit.NET) library.

Generate audit events for MediatR request/response pipeline executions. The behavior captures request, response payloads, exceptions, types, timing and environment metadata into Audit.NET events.

## Install

NuGet Package

To install the package run the following command on the Package Manager Console:

```
PM> Install-Package Audit.MediatR
```

[![NuGet Status](https://img.shields.io/nuget/v/Audit.MediatR.svg?style=flat)](https://www.nuget.org/packages/Audit.MediatR/)
[![NuGet Count](https://img.shields.io/nuget/dt/Audit.MediatR.svg)](https://www.nuget.org/packages/Audit.MediatR/)

## Overview / Usage

The core component is the `AuditPipelineBehavior<TRequest, TResponse>` class. Add it to your MediatR pipeline during service configuration.

### Basic setup

There are multiple ways to set up the auditing behavior. The simplest is to use the `AddMediatRAudit()` extension on IServiceCollection to register the behavior with custom settings.

- Using `AddMediatRAudit()` extension (e.g., in ASP.NET Core):
```c#
// In Startup.cs or wherever you configure DI

services
    .AddMediatR(cfg =>
    {
        // Your MediatR configuration...
    })
    .AddMediatRAudit(c => c
        .CallFilter(ctx => ctx.CallType == MediatRCallType.Request)
        .DataProvider(new FileDataProvider(file => file.Directory(@"C:\LOGS")))
        .IncludeRequest()
        .IncludeResponse()
    );
```

> This configuration adds the audit behavior to the MediatR pipeline, enabling auditing for all requests. The example also shows how to filter which calls to audit and how to include request/response objects.

- Manual configuration, register the behaviors directly:

```c#
// Intercept normal Requests
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuditMediatRBehavior<,>)); 

// Intercept Stream Requests
services.AddTransient(typeof(IStreamPipelineBehavior<,>), typeof(AuditMediatRStreamBehavior<,>)); 

// Create the Audit Data Provider
var dataProvider = new MongoDataProvider(mongo => mongo.ConnectionString("mongodb://localhost:27017").Database("Audit").Collection("Event"));

// Set the configuration
services.AddSingleton(new AuditMediatRConfigurator()
  .DataProvider(dataProvider)
  .IncludeRequest(ctx => ctx.RequestType.GetCustomAttribute<AuditIgnore>() == null)
  .Options);
```

> In this example, we register the `AuditMediatRBehavior` and `AuditMediatRStreamBehavior` directly, we set up a MongoDB data provider, 
no filter is supplied (all requests are audited), and conditionally include request payloads based on the presence of an `AuditIgnore` attribute on the request type.

- Per-request configuration (register the behavior for specific request/response types only):

```c#
builder.Services.AddTransient<IPipelineBehavior<LoginRequest, LoginResponse>>(_ => 
    new AuditMediatRBehavior<LoginRequest, LoginResponse>(c => c.IncludeRequest().IncludeResponse()));
```

> In this example, we register the `AuditMediatRBehavior` only for the `LoginRequest`/`LoginResponse` types, enabling request and response payload capturing specifically for this request type.

## Configuration

Configuration is set via the `AuditMediatROptions` class. 

For example:
```c#
var options = new AuditMediatROptions(c => c
  .CallFilter(ctx => ctx.CallType == MediatRCallType.Request)
  .DataProvider(new FileDataProvider(file => file.Directory(@"C:\LOGS")))
  .IncludeRequest()
  .IncludeResponse());
```

Or you can create the configuration object directly:
```c#
var options = new AuditMediatROptions
{
    CallFilter = ctx => ctx.CallType == MediatRCallType.Request,
    DataProvider = new FileDataProvider(file => file.Directory(@"C:\LOGS")),
    IncludeRequest = _ => true,
    IncludeResponse = _ => true
};
```

### Output

The audit events are stored using a _Data Provider_. You can use one of the [available data providers](https://github.com/thepirat000/Audit.NET#data-providers-included) or implement your own. Please refer to the [data providers](https://github.com/thepirat000/Audit.NET#data-providers) section on Audit.NET documentation.

The Audit Data Provider can be configured in several ways:

- When registering the audit behavior, via the `DataProvider` setting.

For example:
```c#
var dataProvider = new FirestoreDataProvider(firestore => firestore
  .ProjectId("my-project-id")
  .Collection("AuditEvents"));

services
  .AddMediatR(cfg =>
  {
    // Your MediatR configuration...
  })
  .AddMediatRAudit(c => c
    .DataProvider(dataProvider)
    .IncludeRequest()
    .IncludeResponse()
  );
```

- Globally, by setting the `AuditDataProvider` instance through the `Audit.Core.Configuration.DataProvider` static property or the `Audit.Core.Configuration.Use()` methods.

  For example:
```c#
// Using the fluent API
Audit.Core.Configuration.Setup().UseSqlServer(sql => sql...);

// Or just
Audit.Core.Configuration.AuditDataProvider = new SqlDataProvider(...);
```

### Settings

The audit behavior allows to configure the following options (via `AuditMediatROptions` or `AuditMediatRConfigurator`):

- `CallFilter`: A function that receives a `MediatRCallContext` and returns a boolean indicating whether to audit the call. Default is to audit all calls.
- `IncludeRequest`: A function that receives a `MediatRCallContext` and returns a boolean indicating whether to include the request payload in the audit event. Default is false.
- `IncludeResponse`: A function that receives a `MediatRCallContext` and returns a boolean indicating whether to include the response payload in the audit event. Default is false.
- `CreationPolicy`: Allows to set a specific event creation policy. By default the globally configured creation policy is used. See [Audit.NET Event Creation Policy](https://github.com/thepirat000/Audit.NET#event-creation-policy) section for more information.
- `DataProvider`: Allows to set a specific audit data provider. By default the globally configured data provider is used. See [Audit.NET Data Providers](https://github.com/thepirat000/Audit.NET/blob/master/README.md#data-providers) section for more information.
- `AuditScopeFactory`: Allows to set a specific audit scope factory. By default the globally configured [`AuditScopeFactory`](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET/AuditScopeFactory.cs) is used. 

## Output Details

The following table describes the Audit.MediatR output fields:

### [`MediatRCallAction`](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.MediatR/MediatRCallAction.cs). 

Describes a single MediatR call action.

 Field Name | Type | Description | 
| ------------ | ---------------- |  -------------- |
| `CallType` | string | The type of MediatR call (`Request` or `StreamRequest`). |
| `RequestType` | string | The type name of the request. |
| `ResponseType` | string | The type name of the response. |
| `Request` | object | The request payload, if captured. |
| `Response` | object | The response payload, if captured. |
| `IsSuccess` | bool | Indicates whether the call was successful (no exception). |
| `ResponseStream` | List | The response stream payload, if captured (for Stream Requests). |
| `Exception` | string | The exception message, if an exception occurred. |

## Output sample

A simplified sample event:

```json
{
  "Call": {
    "CallType": "Request",
    "RequestType": "GetAllUsersQuery",
    "Request": {},
    "ResponseType": "List<UserResponseDto>",
    "Response": [
      {
        "id": "3220c9af-6668-42cf-84b5-7adc293d1538",
        "userName": "string",
        "password": "************"
      }
    ],
    "IsSuccess": true
  },
  "Environment": {
    "UserName": "UserName",
    "MachineName": "MACHINE",
    "DomainName": "Domain",
    "Culture": "en-US"
  },
  "StartDate": "2025-12-04T04:14:13.4999374Z",
  "StartTimestamp": 1350719300414,
  "EndDate": "2025-12-04T04:14:13.5203022Z",
  "EndTimestamp": 1350719504066,
  "Duration": 20
}
```

## Notes and compatibility

- The behavior respects the global `Configuration.AuditDisabled` flag. If set to `true` no events are produced.
