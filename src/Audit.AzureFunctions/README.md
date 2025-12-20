# Audit.AzureFunctions

Azure Functions (Isolated Worker) auditing middleware for the [Audit.NET](https://github.com/thepirat000/Audit.NET) library.

Generate detailed audit events for function invocations, including trigger type, function name/entry point, input data, timing, environment metadata, bindings, logs/traces, exception, and custom enrichment.

## Install

NuGet Package

To install the package run the following command on the Package Manager Console:

```
PM> Install-Package Audit.AzureFunctions
```

[![NuGet Status](https://img.shields.io/nuget/v/Audit.AzureFunctions.svg?style=flat)](https://www.nuget.org/packages/Audit.AzureFunctions/)
[![NuGet Count](https://img.shields.io/nuget/dt/Audit.AzureFunctions.svg)](https://www.nuget.org/packages/Audit.AzureFunctions/)

## Overview / Usage

Audit.AzureFunctions provides a middleware for auditing Azure Functions (Isolated Worker). 

Add the `AuditAzureFunctionMiddleware` to your Function app's middleware pipeline to automatically capture audit events for each function invocation.

### Basic setup

Register the middleware in your Azure Functions Isolated Worker `Program.cs` and configure Audit.NET:

```c#
using Audit.AzureFunctions; 
using Audit.AzureFunctions.ConfigurationApi; 
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults(builder =>
    {
        // Add Audit.NET Azure Functions middleware 
        builder.UseAuditMiddleware(cfg => cfg
            .AuditWhen(ctx => ctx.FunctionDefinition?.Name != "HealthCheck") // skip health checks
            .EventType("FUNC {name} ({id})")
            .IncludeTriggerInfo()
            .IncludeFunctionDefinition()
            .WithCustomFields(ctx => new()
            {
                ["TraceIdentifier"] = ctx.GetHttpContext()?.TraceIdentifier,
                ["Url"] = ctx.GetHttpContext()?.Request.GetDisplayUrl()
            })
        );
    })
    .ConfigureServices(services =>
    {
        // Configure Audit.NET data provider globally
        Audit.Core.Configuration.Setup()
            .UseFileLogProvider(_ => _
                .Directory(@"C:\logs")
                .FilenameBuilder(ev => $"{ev.EventType}_{ev.StartDate:yyyyMMddHHmmssfff}.json"));
    })
    .Build();

host.Run();
```

Or similarly, if you use FunctionsApplication.CreateBuilder:

```c#
var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.UseAuditMiddleware(cfg => cfg
    .AuditWhen(ctx => ctx.GetTriggerAttribute() is HttpTriggerAttribute)
    .DataProvider(new FileDataProvider(d => d.Directory(@"C:\logs")))
    .IncludeTriggerInfo()
    .IncludeFunctionDefinition());

var host = builder.Build();

host.Run();
```

Notes:
- The `UseAuditMiddleware(...)` extension is provided and takes a configurator action.
- You can configure Audit.NET per-invocation via the options `DataProvider` and `EventCreationPolicy`.


## Configuration

`AuditAzureFunctionOptions` exposes a fluent API to control auditing:

- `AuditWhen(Func<FunctionContext, bool> predicate)`: Predicate to decide whether to audit a given invocation.
- `EventType(string template)`: Sets the event type template. Supports placeholders:
  - `{name}`: the function name.
  - `{id}`: the function id.
- `IncludeTriggerInfo()`: Include information about the function trigger.
- `IncludeFunctionDefinition()`: Include the function definition (entry point, bindings, etc).
- `DataProvider(IAuditDataProvider provider)`: Specify a custom data provider per invocation, overriding the global configuration.
- `EventCreationPolicy(EventCreationPolicy policy)`: Override the creation policy per invocation, overriding the global configuration.
- `AuditScopeFactory(IAuditScopeFactory factory)`: Provide a custom scope factory.
- `WithCustomFields(Func<FunctionContext, IDictionary<string, object>> factory)`: Add custom fields to the function event.

You can also configure Audit.NET globally using `Audit.Core.Configuration.Setup().Use...` or `Audit.Core.Configuration.AuditDataProvider`.

## Output

The audit events are stored using a Data Provider. You can use one of the [available data providers](https://github.com/thepirat000/Audit.NET#data-providers-included) or implement your own. Please refer to the [data providers](https://github.com/thepirat000/Audit.NET#data-providers) section on Audit.NET documentation.

The Audit Data Provider can be configured in several ways:

- Per middleware via the fluent configuration (`DataProvider()`).
- Globally, by setting the `AuditDataProvider` through `Audit.Core.Configuration` or the `Use()` fluent setup.

## Output Details

Audit.AzureFunctions produces structured events of type `AuditEventAzureFunction`.

The Audit Event contains a property `Call` of type `AzureFunctionCall` with details about the function invocation, containing:

Field Name | Type | Description | 
| ------------ | ---------------- |  -------------- |
| `FunctionId` | `string` | Unique identifier of a function, stable across invocations.
| `InvocationId` | `string` | Unique identifier for the specific invocation.
| `FunctionDefinition` | `AzureFunctionDefinition` | Metadata about the function definition (name, entry point, bindings, etc). Only included if `IncludeFunctionDefinition()` is used.
| `BindingData` | `Dictionary<string, object>` | The binding data for the function invocation.
| `Trace` | `AzureFunctionTrace` | Distributed trace information, if available.
| `Trigger` | `AzureFunctionTrigger` | Information about the function trigger (type, data, etc). Only included if `IncludeTriggerInfo()` is used.
| `Exception` | `string` | Exception details if the function failed.
| `IsSuccess` | `bool` | Indicates if the function executed successfully.
| `CustomFields` | `Dictionary<string, object>` | Custom fields added via `WithCustomFields()`.

`AzureFunctionDefinition` contains:

Field Name | Type | Description |
| ------------ | ---------------- |  -------------- |
| `Id` | `string` | Unique identifier of the function.
| `Name` | `string` | The name of the function.
| `EntryPoint` | `string` | The entry point method of the function.
| `Assembly` | `string` | The assembly where the function is defined.
| `Parameters` | `List<AzureFunctionMetadata>` | List of parameters for the function.
| `InputBindings` | `List<AzureFunctionMetadata>` | List of input bindings.
| `OutputBindings` | `List<AzureFunctionMetadata>` | List of output bindings.

`AzureFunctionTrace` contains:

Field Name | Type | Description |
| ------------ | ---------------- |  -------------- |
| `TraceParent` | `string` | The parent trace identifier.
| `TraceState` | `string` | The trace state information.
| `Attributes` | `Dictionary<string, object>` | Additional trace attributes.

`AzureFunctionTrigger` contains:

Field Name | Type | Description |
| ------------ | ---------------- |  -------------- |
| `Type` | `string` | The type of the trigger (e.g., HttpTrigger, TimerTrigger).
| `Attributes` | `Dictionary<string, object>` | Attributes specific to the trigger.

### Output sample

```json
{
  "Call": {
    "FunctionId": "3850673571",
    "InvocationId": "1b4dfe53-93bc-428d-bf14-eeffa862b0c4",
    "FunctionDefinition": {
      "Id": "3850673571",
      "Name": "MyFunction",
      "EntryPoint": "MyFunctionApp.MyFunction.Run",
      "Assembly": "C:\\Test\\MyFunctionApp\\bin\\Debug\\net10.0\\MyFunctionApp.dll",
      "Parameters": [
        {
          "Type": "HttpRequest",
          "Name": "request"
        },
        {
          "Type": "Boolean",
          "Name": "test"
        },
        {
          "Type": "FunctionContext",
          "Name": "context"
        }
      ],
      "InputBindings": [
        {
          "Type": "httpTrigger",
          "Name": "request"
        }
      ],
      "OutputBindings": [
        {
          "Type": "queue",
          "Name": "Messages"
        },
        {
          "Type": "http",
          "Name": "Result"
        }
      ]
    },
    "BindingData": {
      "Name": "TEST",
      "test": "true",
      "Query": "{\"test\":\"true\"}",
      "Headers": "{\"Accept\":\"*/*\",\"Connection\":\"keep-alive\",\"Host\":\"localhost:7067\",\"User-Agent\":\"...\",\"Accept-Encoding\":\"gzip, deflate, br\",\"Content-Type\":\"application/json\",\"Content-Length\":\"49\"}"
    },
    "Trace": {
      "TraceParent": "00-007e0230de04d8033d361298c6723ecd-296b0fe332c027b8-00",
      "TraceState": "",
      "HostInstanceId": "c1c10aca-8a56-443b-840a-4199abfe02f1",
      "ProcessId": "23332",
      "OperationName": "MyFunction",
      "Category": "Function.MyFunction.User"
    },
    "Trigger": {
      "Type": "HttpTrigger",
      "Route": null,
      "Methods": [
        "get",
        "post"
      ],
      "AuthLevel": 2
    },
    "IsSuccess": true
  },
  "EventType": "MyFunction",
  "Environment": {
    "UserName": "User",
    "MachineName": "MACHINE-NAME",
    "DomainName": "DOMAIN",
    "CallingMethodName": "System.Runtime.CompilerServices.AsyncMethodBuilderCore.Start()",
    "AssemblyName": "System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e",
    "Culture": "en-US"
  },
  "StartDate": "2025-12-20T19:41:13.9548078Z",
  "EndDate": "2025-12-20T19:41:13.9850255Z",
  "Duration": 30
}
```

## Notes and compatibility

- Works with Azure Functions Isolated Worker (`Microsoft.Azure.Functions.Worker.Core`) by adding `AuditAzureFunctionMiddleware` to the worker pipeline.
- Configure via `UseAuditMiddleware(cfg => ...)` to control auditing behavior and enrichment.
- For custom enrichment/extraction, use `WithCustomFields`.
- Honors global Audit.NET configuration (provider, creation policy, `AuditDisabled`).
