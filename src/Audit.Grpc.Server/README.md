# Audit.Grpc.Server

gRPC server audit interceptor for [Audit.NET library](https://github.com/thepirat000/Audit.NET).

Generates audit events by intercepting gRPC server calls (uses `Grpc.Core.Interceptors`). Records unary and streaming server calls (request/response payloads, metadata, status, exceptions) into Audit.NET events.

## Install

NuGet Package

To install the package run the following command on the Package Manager Console:

```
PM> Install-Package Audit.Grpc.Server
```

[![NuGet Status](https://img.shields.io/nuget/v/Audit.Grpc.Server.svg?style=flat)](https://www.nuget.org/packages/Audit.Grpc.Server/)
[![NuGet Count](https://img.shields.io/nuget/dt/Audit.Grpc.Server.svg)](https://www.nuget.org/packages/Audit.Grpc.Server/)

## Overview / Usage

The core entry point is the `AuditServerInterceptor` (inherits `Grpc.Core.Interceptors.Interceptor`). Create an instance (optionally with the fluent configurator) and register it in your gRPC server pipeline so requests are audited.

Basic example (using ASP.NET Core):

```csharp
// In Startup.cs or wherever you configure the gRPC server

public void ConfigureServices(IServiceCollection services)
{
    // Register interceptor 
    services.AddSingleton(_ =>
        new AuditServerInterceptor(cfg => cfg
            .CallFilter(ctx => ctx.Method.StartsWith("/demo.DemoService/"))
            .IncludeResponsePayload(ctx => ctx.GetServerCallAction().MethodType == "Unary")
            .IncludeTrailers()
        ));

    // Ensure gRPC services use the interceptor
    services.AddGrpc(options =>
    {
        options.Interceptors.Add<AuditServerInterceptor>();
    });
}
```

Note The CallFilter in the example above restricts auditing to methods in the `demo.DemoService` service only. Also, response payloads are captured only for unary methods.

## Configuration

### Output

The Audit Events generated are stored via a _Data Provider_. You can either use one of the [available data providers](https://github.com/thepirat000/Audit.NET#data-providers-included) or implement your own.

The Audit Data Provider can be configured in several ways:

- When creating or registering the `interceptor` instance by setting the `AuditDataProvider` setting.
  For example:
```c#
// Register interceptor 
```

- Globally, by setting the `AuditDataProvider` instance through the `Audit.Core.Configuration.DataProvider` static property or the `Audit.Core.Configuration.Use()` methods.

  For example:
```c#
Audit.Core.Configuration.Setup().UseSqlServer(sql => sql...);
```

### Settings

Use the constructor overload accepting a configuration action that receives `IAuditServerInterceptorConfigurator`. 

Available options:

- `CallFilter(Func<CallContext, bool>)`: filter which calls to audit; return `true` to audit the call, `false` to skip it. Default: audit all calls.
- `IncludeRequestHeaders(bool|Func<CallContext,bool>)`: include request metadata headers. Default: `false`.
- `IncludeTrailers(bool|Func<CallContext,bool>)`: include trailing metadata sent by the server. Default: `false`.
- `IncludeRequestPayload(bool|Func<CallContext,bool>)`: include request message payload (or stream items). Default: `false`. 
- `IncludeResponsePayload(bool|Func<CallContext,bool>)`: include response message payload (or stream items). Default: `false`.
- `EventType(string|Func<CallContext,string>)`: customize event type. Placeholders: `{method}` replaced by the gRPC method full name (e.g., `"/package.Service/Method"`). Default is `"{method}"`.
- `CreationPolicy(EventCreationPolicy)`: Set event creation policy for this interceptor (overrides global setting). Default: uses global setting.
- `AuditDataProvider(IAuditDataProvider|Func<CallContext,IAuditDataProvider>))`: override the data provider used to persist events. Default: uses the globally configured `IAuditDataProvider` in `Audit.Core.Configuration`.
- `AuditScopeFactory(IAuditScopeFactory)`: Override the audit scope factory used to create audit scopes. Default: uses the globally configured `IAuditScopeFactory` in `Audit.Core.Configuration`.

The same properties can be set directly on the `AuditServerInterceptor` instance (`CallFilter`, `IncludeRequestHeaders`, etc.).

## Output

Audit Events produced are of type `AuditEventGrpcServer` (inherits `Audit.Core.AuditEvent`) and contain an `Action` of type `GrpcServerCallAction`. 

Fields included:

- `MethodType`: gRPC method type (`Unary`, `ClientStreaming`, `ServerStreaming`, `DuplexStreaming`).
- `Method`: Fully qualified method name.
- `Peer`: Caller peer address (e.g., `"ipv4:127.0.0.1:12345"`).
- `Deadline`: Call deadline, when available.
- `RequestHeaders`: Request Header metadata (`GrpcMetadata` list), when enabled. 
- `Trailers`: Trailing metadata sent by the server (`GrpcMetadata` list), when enabled. 
- `Request`: Request message for unary or server-streaming request types.
- `RequestStream`: Captured messages read by the server in client or duplex streaming (list), when enabled.
- `Response`: Response message for unary or client-streaming response types.
- `ResponseStream`: Captured messages written by the server in server or duplex streaming (list), when enabled.
- `Exception`: Exception details on failure.
- `IsSuccess`: Indicates success (true/false).
- `StatusCode` / `StatusDetail`: gRPC `Status` details.

`GrpcMetadata` contains:
- `Key`: Metadata key.
- `Value`: String value (for non-binary entries).
- `ValueBytes`: byte[] for binary entries.
- `IsBinary`: Whether the entry is binary.

## Output sample

A simplified sample event (unary call):

```json
{
  "EventType": "/Greeter/SayHello",
  "StartDate": "2025-11-25T12:34:56.789Z",
  "EndDate": "2025-11-25T12:34:56.900Z",
  "Duration": 111,
  "Action": {
    "MethodType": "Unary",
    "ServiceName": "Greeter",
    "MethodName": "SayHello",
    "FullName": "/Greeter/SayHello",
    "Peer": "ipv4:127.0.0.1:54321",
    "RequestHeaders": [
      {
        "Key": "authorization",
        "IsBinary": false
      }
    ],
    "Request": {
      "Name": "Alice"
    },
    "Response": {
      "Message": "Hello Alice"
    },
    "IsSuccess": true,
    "StatusCode": "OK",
    "StatusDetail": ""
  }
}
```

A streaming call will include `RequestStream` and/or `ResponseStream` arrays with the captured messages if payload capture is enabled.

## Event creation lifecycle

The interceptor uses `AuditScope` to persist events. By default it uses the globally configured `AuditScopeFactory` and `IAuditDataProvider`. You can override them using the configurator or by setting `AuditServerInterceptor.DataProvider` / `AuditServerInterceptor.AuditScopeFactory` directly.

`EventCreationPolicy` controls when events are inserted/updated (e.g., `InsertOnStartInsertOnEnd`, `InsertOnStartReplaceOnEnd`, etc.). If not set, the global policy applies.

## Custom Action example 

This is an example showing how to enrich `GrpcServerCallAction` with a custom action to add fields during interception. 

```csharp
// On application startup

Audit.Core.Configuration.AddOnSavingAction(scope =>
{
    var action = scope.GetServerCallAction();
    var callContext = action.GetServerCallContext();
    var httpContext = callContext.GetHttpContext();

    action.CustomFields["TraceId"] = httpContext.TraceIdentifier;
});	
```

## Notes and compatibility

- Designed for `Grpc.Core` server interceptors (`Grpc.Core.Interceptors`).
- Large binary payloads and sensitive data: be careful when enabling payload and metadata capture in production. Use `CallFilter` and predicate-based includes to limit captured data.
- Respects the global `Configuration.AuditDisabled` flag. If set to `true` no events are produced.
- Response headers and trailers are captured on server for asynchronous and streaming pipelines. 
- Blocking unary calls will not capture metadata (headers/trailers) since they are not exposed in the synchronous API. Only calls to asynchronous methods will include headers/trailers on the audit event.
