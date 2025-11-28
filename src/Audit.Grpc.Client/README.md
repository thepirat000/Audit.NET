# Audit.Grpc.Client

**gRPC client audit interceptor for [Audit.NET library](https://github.com/thepirat000/Audit.NET).**

Generate audit events by intercepting gRPC client calls (uses `Grpc.Core.Interceptors`). The package records unary and streaming client calls (request/response, metadata, status, exceptions) into Audit.NET events.

## Install

NuGet Package

To install the package run the following command on the Package Manager Console:

```
PM> Install-Package Audit.Grpc.Client
```

[![NuGet Status](https://img.shields.io/nuget/v/Audit.Grpc.Client.svg?style=flat)](https://www.nuget.org/packages/Audit.Grpc.Client/)
[![NuGet Count](https://img.shields.io/nuget/dt/Audit.Grpc.Client.svg)](https://www.nuget.org/packages/Audit.Grpc.Client/)

## Overview / Usage

The core entry point is the `AuditClientInterceptor` (inherits `Grpc.Core.Interceptors.Interceptor`). Create an instance (optionally with the fluent configurator) and attach it to your call invoker or channel so the generated client uses the interceptor.

Basic example (using direct instantiation):

```c#
var interceptor = new Audit.Grpc.Client.AuditClientInterceptor(cfg => cfg 
  .IncludeRequestPayload()
  .IncludeResponsePayload() 
  .IncludeRequestHeaders() 
  .IncludeResponseHeaders() 
  .IncludeTrailers() 
  .EventType("/{service}/{method}"));

// Create channel / call invoker and attach interceptor 
var channel = new Channel("localhost", 50051, ChannelCredentials.Insecure); 
var invoker = channel.Intercept(interceptor);

// Create generated client using the intercepted invoker 
var client = new MyGrpcService.MyGrpcServiceClient(invoker);

// Make calls normally; audit events will be created automatically 
var reply = client.MyUnaryMethod(new MyRequest { ... });
```

DI-based example (e.g., in ASP.NET Core):
```c#
// In Startup.cs or wherever you configure DI

services
  .AddGrpcClient<MyGrpcService.MyGrpcServiceClient>()
    .AddInterceptor(_ => new AuditClientInterceptor(cfg => cfg
      .IncludeRequestHeaders()
      .IncludeRequestPayload()
      .IncludeResponseHeaders()
      .IncludeResponsePayload()));
```

## Configuration

### Output

The Audit Events are stored via a _Data Provider_. You can either use one of the [available data providers](https://github.com/thepirat000/Audit.NET#data-providers-included) or implement your own.

The Audit Data Provider can be configured in several ways:

- When creating or registering the `interceptor` instance by setting the `AuditDataProvider` setting.
  For example:
```c#
var dataProvider = new FirestoreDataProvider(firestore => firestore
  .ProjectId("my-project-id")
  .Collection("AuditEvents"));

services
  .AddGrpcClient<MyGrpcService.MyGrpcServiceClient>()
  .AddInterceptor(_ => new AuditClientInterceptor(cfg => cfg
    .AuditDataProvider(dataProvider)
    .IncludeRequestHeaders()));
```

- Globally, by setting the `AuditDataProvider` instance through the `Audit.Core.Configuration.DataProvider` static property or the `Audit.Core.Configuration.Use()` methods.

  For example:
```c#
Audit.Core.Configuration.Setup().UseSqlServer(sql => sql...);
```

### Settings

Use the constructor overload accepting a configuration action that receives `IAuditServerInterceptorConfigurator`. 

Available options:

- `CallFilter(Func<CallContext, bool>)`: Filter which calls to audit; return `true` to audit the call, `false` to skip it. Default: audit all calls.
- `IncludeRequestHeaders(bool|Func<CallContext,bool>)`: Include request headers. Default: `false`.
- `IncludeResponseHeaders(bool|Func<CallContext,bool>)`: Include response headers. Default: `false`.
- `IncludeTrailers(bool|Func<CallContext,bool>)`: Include trailing metadata. Default: `false`.
- `IncludeRequestPayload(bool|Func<CallContext,bool>)`: Include request message payload. Default: `false`.
- `IncludeResponsePayload(bool|Func<CallContext,bool>)`: Include response message payload. Default: `false`.
- `EventType(string|Func<CallContext,string>)`: Customize event type; placeholders: `{service}`, `{method}`. Default: `"/{service}/{method}"`.
- `CreationPolicy(EventCreationPolicy)`: Set event creation policy for this interceptor (overrides global setting). Default: uses global setting. 
- `AuditDataProvider(IAuditDataProvider|Func<CallContext,IAuditDataProvider>)`: Override the data provider used to persist events. Default: uses the globally configured `IAuditDataProvider` in `Audit.Core.Configuration`.
- `AuditScopeFactory(IAuditScopeFactory)`: Override the audit scope factory used to create audit scopes. Default: uses the globally configured `IAuditScopeFactory` in `Audit.Core.Configuration`.

Example using the configurator fluent API:

```c#
var interceptor = new Audit.Grpc.Client.AuditClientInterceptor(cfg => cfg
  .CallFilter(ctx => ctx.Method.Type == MethodType.Unary)
  .IncludeRequestPayload()
  .IncludeResponsePayload()
  .EventType(ctx => $"/audited/{ctx.Method.ServiceName}/{ctx.Method.Name}")
  .CreationPolicy(EventCreationPolicy.InsertOnEnd));
```

You can also set the same properties directly on the `AuditClientInterceptor` instance (properties like `CallFilter`, `IncludeRequestHeaders`, etc. are exposed).

## Output

Events produced are of type `AuditEventGrpcClient` (inherits `Audit.Core.AuditEvent`) and contain an `Action` of type `GrpcClientCallAction`. 

Fields included:

- `MethodType`: gRPC method type (`Unary`, `ClientStreaming`, `ServerStreaming`, `DuplexStreaming`).
- `ServiceName`: Service name (unqualified).
- `MethodName`: Method name (unqualified).
- `FullName`: Full method name (e.g., `"/package.Service/Method"`).
- `Host`: Target host.
- `Deadline`:  Call deadline (if provided).
- `RequestHeaders`: Request Header metadata (`GrpcMetadata` list), when enabled.  
- `ResponseHeaders`: Response Header metadata (`GrpcMetadata`), when enabled.
- `Trailers`: Trailing metadata sent by the server (`GrpcMetadata` list), when enabled.
- `RequestType` / `ResponseType`: CLR type names of request/response messages. 
- `Request`: Request message (populated if `IncludeRequestPayload`).
- `RequestStream`: Captured messages sent by client streaming (populated if `IncludeRequestPayload`).
- `Response`: Response message (populated if `IncludeResponsePayload`).
- `ResponseStream`: Captured messages received by server streaming (populated if `IncludeResponsePayload`).
- `Exception`: Exception details on failure.
- `IsSuccess`: Indicates success (true/false).
- `StatusCode` / `StatusDetail`:  gRPC `Status` details.

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
    "Host": "localhost:50051",
    "RequestType": "MyProto.HelloRequest",
    "ResponseType": "MyProto.HelloReply",
    "RequestHeaders": [
      {
        "Key": "request-header",
        "Value": "value",
        "IsBinary": false
      }
    ],
    "ResponseHeaders": [
      {
        "Key": "server-header",
        "Value": "v",
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

The interceptor uses `AuditScope` creation to persist events. By default it uses the globally configured `AuditScopeFactory` and `IAuditDataProvider`. You can override them using the configurator or by setting `AuditClientInterceptor.DataProvider` / `AuditClientInterceptor.AuditScopeFactory` directly.

`EventCreationPolicy` can be set to control when events are inserted/updated (e.g., `InsertOnStartInsertOnEnd`, `InsertOnStartReplaceOnEnd`, etc.). If not set, the global creation policy applies.

## Notes and compatibility

- This package is designed for `Grpc.Core` client interceptors (`Grpc.Core.Interceptors`).
- Large binary payloads and sensitive data: be careful when enabling payload and metadata capture in production. Use `CallFilter` and the predicate-based include methods to limit captured data.
- The interceptor respects the global `Configuration.AuditDisabled` flag. If set to `true` no events are produced.

Important note about blocking unary calls
- Response headers and trailers are not captured for blocking (synchronous) unary calls. The `Grpc.Core` interceptor API does not expose server-sent response metadata on the synchronous/blocking unary call path, so the interceptor cannot reliably obtain response headers or trailing metadata in that scenario.
- To capture response headers and trailers prefer using the asynchronous unary API (for example, `UnaryCallAsync`) or any streaming call types (`ClientStreaming`, `ServerStreaming`, `DuplexStreaming`), where metadata is exposed and captured by the interceptor.
