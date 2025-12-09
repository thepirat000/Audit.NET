# Audit.Hangfire

Hangfire auditing filter for the [Audit.NET](https://github.com/thepirat000/Audit.NET) library.

Generate detailed audit events for Hangfire job creation (enqueue/schedule) and job execution (server processing), including job type/method, arguments, state transitions, timing, environment metadata, result/exception, and continuation information.

## Install

NuGet Package

To install the package run the following command on the Package Manager Console:

```
PM> Install-Package Audit.Hangfire
```

[![NuGet Status](https://img.shields.io/nuget/v/Audit.Hangfire.svg?style=flat)](https://www.nuget.org/packages/Audit.Hangfire/)
[![NuGet Count](https://img.shields.io/nuget/dt/Audit.Hangfire.svg)](https://www.nuget.org/packages/Audit.Hangfire/)

## Overview / Usage

Audit.Hangfire provides two filters:

- `AuditJobCreationFilterAttribute`: Captures job creation/enqueue/schedule events (client side).
- `AuditJobExecutionFilterAttribute`: Captures job execution events (server side).

Both filters create `AuditScope`s and write Audit.NET events using the configured Data Provider and Creation Policy. You can add the filters globally (for all jobs) or per job/class.

### Basic setup

Add the filters to Hangfire global filters and configure the Audit.NET data provider. Examples below show different ways to configure:

- Global registration (Using extensions `AddAuditJobCreationFilter()` and `AddAuditJobExecutionFilter()`):

```
public class Startup 
{ 
  public void ConfigureServices(IServiceCollection services) 
  { 
    // Configure Audit.NET data provider
    Audit.Core.Configuration.Setup().UseFileLogProvider(c => c.Directory(@"C:\temp\logs"))

    // Add hangfire services including both Audit filters
    services.AddHangfire(hf => hf
      .UseMemoryStorage()
        .AddAuditJobCreationFilter(cfg => cfg
          .AuditWhen(ctx => ctx.Job.Method.Name == "SendEmail")
          .IncludeParameters())
        .AddAuditJobExecutionFilter(cfg => cfg
          .AuditWhen(ctx => ctx.BackgroundJob.Job.Method.Name == "SendEmail")));
    }
}
```

- Global registration using filter instances:
```c#
GlobalJobFilters.Filters.Add(new AuditJobCreationFilterAttribute(options => options
    .EventType("CREATE {type}.{method}")
    .IncludeParameters()
    .AuditWhen(ctx => ctx.Job.Method.Name == "SendEmail")));

GlobalJobFilters.Filters.Add(new AuditJobExecutionFilterAttribute(options => options
    .EventType("EXECUTE {type}.{method}")));
```

- Per job/class registration (Using attributes):

You can annotate a job class or method with the attributes `[AuditJobExecutionFilter]` and/or `[AuditJobCreationFilter]` to enable auditing for that specific job or method.

```
[AuditJobExecutionFilter(ExcludeArguments = true)]
public class EmailJobs
{
    public void SendEmail(string to, string subject, string body)
    {
        // Job implementation
    }
}
```

## Configuration

You can configure each filter via a fluent configurator. The creation and execution filters share common concepts:

- Predicate to decide when to audit.
- Control inclusion/exclusion of job arguments and client parameters.
- Customize event type names.
- Optionally provide an Audit.NET `IAuditDataProvider` and `EventCreationPolicy`.
- Optionally override `IAuditScopeFactory`.

### Job creation filter options

The `AuditJobCreationFilterAttribute` uses `AuditJobCreationOptions`. Available configuration methods:

- `AuditWhen(Func<CreateContext, bool> predicate)`: Predicate returning true to audit, false to skip. Use this to filter job creation events.
- `IncludeParameters`: Include the Hangfire client `CreateContext.Parameters` in the audit event. Defaults to false.
- `ExcludeArguments`: Exclude job arguments (`Job.Args`) from the audit event. Defaults to false (include args).
- `EventType`: Sets the event type template. Supports placeholders:
  - `{type}`: job type name (declaring type)
  - `{method}`: job method name  
  Default: `"{type}.{method}"`. 
- `DataProvider`: Sets the Audit.NET data provider per job.
- `EventCreationPolicy`: Controls when events are written (InsertOnStartInsertOnEnd, InsertOnEnd, Manual, etc.). Defaults to the global configuration.
- `AuditScopeFactory`: Overrides the default scope factory if needed.
- `WithCustomFields`: Allows adding custom fields to the audit job execution event.
- 
Example:

```c#
services.AddHangfire(hf => hf
    .UseMemoryStorage()
    .AddAuditJobCreationFilter(cfg => cfg
        .AuditWhen(ctx => ctx.Job.Method.Name != "SendEmail").IncludeParameters()
        .EventType("{type}.{method}")
        .DataProvider(new FileDataProvider(c => c.Directory(@"C:\logs").FilenamePrefix("CREATE_")))
        .IncludeParameters()
        .ExcludeArguments()
        .WithCustomFields(ctx => new() { ["ConnectionType"] = ctx.Connection.GetType().Name })));
```

### Job execution filter options

The `AuditJobExecutionFilterAttribute` uses `AuditJobExecutionOptions`. Available configuration methods:

- `AuditWhen(Func<PerformingContext, bool> predicate)`: Decide whether to audit a job execution based on the server-side `PerformingContext`.
- `ExcludeArguments`: Exclude job arguments from execution audit events. Defaults to false (include args).
- `EventType`: Event type template with `{type}` and `{method}` placeholders.
- `DataProvider`: Set the data provider per execution or globally.
- `EventCreationPolicy`: Event creation policy for execution auditing. Defaults to globally configured policy.
- `AuditScopeFactory`: Overrides the scope factory.
- `WithCustomFields`: Allows adding custom fields to the audit job execution event.

Example:

```c#
services.AddHangfire(hf => hf
    .UseMemoryStorage()
    .AddAuditJobExecutionFilter(cfg => cfg
        .AuditWhen(ctx => ctx.BackgroundJob.Job.Method.Name == "SendEmail")
        .DataProvider(new FileDataProvider(c => c.Directory(@"C:\logs").FilenamePrefix("EXECUTE_")))
        .EventType("{type}.{method}")
        .ExcludeArguments()
        .WithCustomFields(ctx => new() { ["StorageType"] = ctx.Storage.GetType().Name })));
```

### Output

The audit events are stored using a Data Provider. You can use one of the [available data providers](https://github.com/thepirat000/Audit.NET#data-providers-included) or implement your own. Please refer to the [data providers](https://github.com/thepirat000/Audit.NET#data-providers) section on Audit.NET documentation.

The Audit Data Provider can be configured in several ways:

- Per filter via the fluent configuration (examples above).
- Globally, by setting the `AuditDataProvider` instance through the `Audit.Core.Configuration.DataProvider` static property or the `Audit.Core.Configuration.Use()` methods.

For example:
```c#
// Using the fluent API
Audit.Core.Configuration.Setup().UseSqlServer(sql => sql...);

// Or just
Audit.Core.Configuration.AuditDataProvider = new SqlDataProvider(...);
```

## Output Details

Audit.Hangfire produces structured events. Two event types exist:

- `AuditEventHangfireJobCreation`: Emitted when a job is created/enqueued/scheduled by a Hangfire client.
- `AuditEventHangfireJobExecution`: Emitted when a job is executed by a Hangfire server.

### Hangfire Job Creation Event

Describes a single job creation action.

Field Name | Type | Description | 
| ------------ | ---------------- |  -------------- |
| `JobId` | string | Hangfire job ID.
| `TypeName` | string | Type name of the job's target class.
| `MethodName` | string | Method name being scheduled.
| `InitialState` | string | Initial Hangfire state (e.g., `Enqueued`, `Scheduled`, `Awaiting`).
| `IsSuccess` | bool | `true` if the job was created/enqueued successfully.
| `Exception` | string | Exception message/details if job creation failed.
| `Canceled` | bool | `true` if the job creation was canceled.
| `CreatedAt` | DateTime | Timestamp when the job was created.
| `ScheduledAt` | DateTime | For scheduled jobs, the timestamp when the job was scheduled to run.
| `EnqueueAt` | DateTime | For scheduled jobs, the timestamp when the job is planned to be enqueued.
| `EnqueuedAt` | DateTime | For fire-and-forget jobs, the timestamp when the job was enqueued.
| `Queue` | string | Queue name where the job was enqueued.
| `Args` | object[] | Job method arguments (when `IncludeArguments` is true).
| `Parameters` | Dictionary<string, object> | Captured job parameters (context.Parameters).
| `Continuation` | `ContinuationData` | Continuation metadata (parent job id, options).


### Hangfire Job Execution Event

Describes job processing on a server.

Field Name | Type | Description | 
| ------------ | ---------------- |  -------------- |
| `JobId` | string | Hangfire job ID.
| `TypeName` | string | Type name of the job's target class.
| `MethodName` | string | Method name being scheduled.
| `ServerId` | string | Identifier of the Hangfire server processing the job.
| `IsSuccess` | bool | `true` if the job executed successfully.
| `Exception` | string | Exception message/details if job execution failed.
| `Canceled` | bool | `true` if the job execution was canceled.
| `CreatedAt` | DateTime | Timestamp when the job was created.
| `Result` | object | Result returned by the job method (if any).
| `Args` | object[] | Job method arguments (when `IncludeArguments` is true).

## Output sample

Below is a simplified sample of a creation and execution event using the `FileDataProvider` (indentation enabled). 

Creation: 

```json
{
  "JobCreation": {
    "JobId": "a22be1b8-bdfa-43d2-b1a7-2e990a7901e6",
    "TypeName": "Samples.Hangfire.EmailService",
    "MethodName": "System.String SendEmail(System.String, System.String)",
    "InitialState": "Enqueued",
    "IsSuccess": true,
    "Canceled": false,
    "CreatedAt": "2025-12-09T00:03:38.6702197Z",
    "EnqueuedAt": "2025-12-09T00:03:38.6410516Z",
    "Queue": "alpha",
    "Args": [
      "test@test.com",
      "This is a test"
    ]
  },
  "EventType": "EmailService.SendEmail",
  "Environment": {
    ...
  },
  "StartDate": "2025-12-09T00:03:38.6690163Z",
  "StartTimestamp": 1096643716060,
  "EndDate": "2025-12-09T00:03:38.7180343Z",
  "EndTimestamp": 1096643853577,
  "Duration": 49
}
```

Execution:

```json
{
  "JobExecution": {
    "JobId": "a22be1b8-bdfa-43d2-b1a7-2e990a7901e6",
    "TypeName": "Samples.Hangfire.EmailService",
    "MethodName": "System.String SendEmail(System.String, System.String)",
    "ServerId": "machine-name:24644:4d57a55b-8188-4f48-af1d-d3cfd2b62b78",
    "IsSuccess": true,
    "Canceled": false,
    "CreatedAt": "2025-12-09T00:03:38.6702197Z",
    "Result": "OK",
    "Args": [
      "test@test.com",
      "This is a test"
    ]
  },
  "EventType": "EmailService.SendEmail",
  "Environment": {
    ...
  },
  "StartDate": "2025-12-09T00:03:38.7475811Z",
  "StartTimestamp": 1096644148115,
  "EndDate": "2025-12-09T00:03:38.7517064Z",
  "EndTimestamp": 1096644189367,
  "Duration": 4
}
```

## Notes and compatibility

- Add filters globally with `GlobalJobFilters.Filters.Add(new AuditJobCreationFilterAttribute(...))` and `GlobalJobFilters.Filters.Add(new AuditJobExecutionFilterAttribute(...))`.
- You can annotate job classes/methods with `[AuditJobCreationFilter]` and/or `[AuditJobExecutionFilter]` for selective auditing.
- The filters use `AuditScope` and honor global Audit.NET configuration (provider, creation policy, `AuditDisabled`).
- For custom enrichment, use `WithCustomFields` configuration to push extra fields into the audit job events.
- The behavior respects the global `Configuration.AuditDisabled` flag. If set to `true` no events are produced.
