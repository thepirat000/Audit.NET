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

## Configuring the interceptor

Use the constructor overload accepting a configuration action that receives `IAuditServerInterceptorConfigurator`. 

Available options:

- `CallFilter(Func<CallContext, bool>)`: decide which calls to audit (default: audit all).
- `IncludeRequestHeaders(bool|Func<CallContext,bool>)`: include incoming request metadata headers.
- `IncludeTrailers(bool|Func<CallContext,bool>)`: include trailing metadata written by the server.
- `IncludeRequestPayload(bool|Func<CallContext,bool>)`: include request message payload (or stream items).
- `IncludeResponsePayload(bool|Func<CallContext,bool>)`: include response message payload (or stream items).
- `EventType(string|Func<CallContext,string>)`: customize event type; placeholders: `{method}`. Default: `"{method}"`.
- `CreationPolicy(EventCreationPolicy)`: override event creation policy for this interceptor.
- `AuditDataProvider(IAuditDataProvider)`: override data provider for events created by this interceptor.
- `AuditScopeFactory(IAuditScopeFactory)`: override the scope factory used to create `AuditScope`.

The same properties can be set directly on the `AuditServerInterceptor` instance (`CallFilter`, `IncludeRequestHeaders`, etc.).

## What is captured (Output model)

Events produced are of type `AuditEventGrpcServer` (inherits `Audit.Core.AuditEvent`) and contain an `Action` of type `GrpcServerCallAction`. Key fields include:

- `MethodType`: `"Unary"`, `"ClientStreaming"`, `"ServerStreaming"`, `"DuplexStreaming"`.
- `Method`: fully qualified method name.
- `Peer`: caller peer/endpoint (e.g., `"ipv4:127.0.0.1:12345"`), when available.
- `Deadline`: call deadline (if provided).
- `RequestHeaders`: list of metadata entries (`GrpcMetadata`).
- `Trailers`: trailing metadata (`GrpcMetadata`) sent by the server.
- `Request`: request message for unary calls (when enabled).
- `RequestStream`: captured messages received in client or duplex streaming (list).
- `Response`: response message for unary or client-streaming response types.
- `ResponseStream`: captured messages written by the server in server or duplex streaming (list).
- `Exception`: exception details on failure.
- `IsSuccess`: indicates success (derived from `Status`).
- `StatusCode` / `StatusDetail`: gRPC `Status` details.

`GrpcMetadata` contains:
- `Key`: metadata key.
- `Value`: string value (for non-binary entries).
- `ValueBytes`: byte[] for binary entries.
- `IsBinary`: whether the entry is binary.

Streaming calls are wrapped to capture individual messages:
- Client-side request reads are captured into `RequestStream` via `ClientStreamReaderWrapper<T>`.
- Server-side response writes are captured into `ResponseStream` via `ServerStreamWriterWrapper<T>`.

## Event creation lifecycle

The interceptor uses `AuditScope` to persist events. By default it uses the globally configured `AuditScopeFactory` and `IAuditDataProvider`. You can override them using the configurator or by setting `AuditServerInterceptor.DataProvider` / `AuditServerInterceptor.AuditScopeFactory` directly.

`EventCreationPolicy` controls when events are inserted/updated (e.g., `InsertOnStartInsertOnEnd`, `InsertOnStartReplaceOnEnd`, etc.). If not set, the global policy applies.

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
