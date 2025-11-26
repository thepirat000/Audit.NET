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

## Configuring the interceptor

You can use the constructor overload accepting a configuration action that receives `IAuditClientInterceptorConfigurator`. Examples of available options:

- `CallFilter(Func<CallContext, bool>)`: decide which calls to audit (default: audit all).
- `IncludeRequestHeaders(bool|Func<CallContext,bool>)`: include request metadata headers.
- `IncludeResponseHeaders(bool|Func<CallContext,bool>)`: include response headers.
- `IncludeTrailers(bool|Func<CallContext,bool>)`: include trailing metadata.
- `IncludeRequestPayload(bool|Func<CallContext,bool>)`: include request message payload.
- `IncludeResponsePayload(bool|Func<CallContext,bool>)`: include response message payload.
- `EventType(string|Func<CallContext,string>)`: customize event type; placeholders: `{service}`, `{method}`. Default: `"/{service}/{method}"`.
- `CreationPolicy(EventCreationPolicy)`: override event creation policy for this interceptor.
- `AuditDataProvider(IAuditDataProvider)`: override data provider for events created by this interceptor.
- `AuditScopeFactory(IAuditScopeFactory)`: override the scope factory used to create `AuditScope`.

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

## What is captured (Output model)

Events produced are of type `AuditEventGrpcClient` (inherits `Audit.Core.AuditEvent`) and contain an `Action` of type `GrpcClientCallAction`. The key fields available:

- `MethodType`:  `"Unary"`, `"ClientStreaming"`, `"ServerStreaming"`, or `"DuplexStreaming"`.
- `ServiceName`:  service name (unqualified).
- `MethodName`:  method name (unqualified).
- `FullName`:  fully qualified method name.
- `Host`:  target host used by the call.
- `Deadline`:  call deadline (if provided).
- `RequestHeaders`:  list of metadata entries (`GrpcMetadata`).
- `ResponseHeaders`:  list of metadata entries (`GrpcMetadata`).
- `Trailers`:  trailing metadata (`GrpcMetadata`).
- `RequestType` / `ResponseType`:  CLR type names for request/response.
- `Request`:  request message (populated if `IncludeRequestPayload`).
- `RequestStream`:  captured messages sent by client streaming (list).
- `Response`:  response message for unary or client-streaming response types.
- `ResponseStream`:  captured messages received in server streaming (list).
- `Exception`:  exception details on failure.
- `IsSuccess`:  indicates success (derived from `Status`).
- `StatusCode` / `StatusDetail`:  gRPC `Status` details.

`GrpcMetadata` contains:
- `Key`:  metadata key.
- `Value`:  string value (for non-binary entries).
- `ValueBytes`:  byte[] for binary entries.
- `IsBinary`:  whether the entry is binary.

Streaming calls are wrapped to capture individual messages:
- Client-side streaming writes are captured into `RequestStream` via `ClientStreamWriterWrapper<T>`.
- Server-side response reads are captured into `ResponseStream` via `ServerStreamWriterWrapper<T>`.

## Event creation lifecycle

The interceptor uses `AuditScope` creation to persist events. By default it uses the globally configured `AuditScopeFactory` and `IAuditDataProvider`. You can override them using the configurator or by setting `AuditClientInterceptor.DataProvider` / `AuditClientInterceptor.AuditScopeFactory` directly.

`EventCreationPolicy` can be set to control when events are inserted/updated (e.g., `InsertOnStartInsertOnEnd`, `InsertOnStartReplaceOnEnd`, etc.). If not set, the global creation policy applies.

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

## Notes and compatibility

- This package is designed for `Grpc.Core` client interceptors (`Grpc.Core.Interceptors`).
- Large binary payloads and sensitive data: be careful when enabling payload and metadata capture in production. Use `CallFilter` and the predicate-based include methods to limit captured data.
- The interceptor respects the global `Configuration.AuditDisabled` flag. If set to `true` no events are produced.

Important note about blocking unary calls
- Response headers and trailers are not captured for blocking (synchronous) unary calls. The `Grpc.Core` interceptor API does not expose server-sent response metadata on the synchronous/blocking unary call path, so the interceptor cannot reliably obtain response headers or trailing metadata in that scenario.
- To capture response headers and trailers prefer using the asynchronous unary API (for example, `UnaryCallAsync`) or any streaming call types (`ClientStreaming`, `ServerStreaming`, `DuplexStreaming`), where metadata is exposed and captured by the interceptor.
