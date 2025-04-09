# Audit.NET.NLog
**NLog storage provider for [Audit.NET library](https://github.com/thepirat000/Audit.NET)** (An extensible framework to audit executing operations in .NET).

Store the audit events using [NLog™](https://nlog-project.org/).

## Install

**NuGet Package** 

To install the package, run the following command on the Package Manager Console:

```
PM> Install-Package Audit.NET.NLog
```

[![NuGet Status](https://img.shields.io/nuget/v/Audit.NET.NLog.svg?style=flat)](https://www.nuget.org/packages/Audit.NET.NLog/)
[![NuGet Count](https://img.shields.io/nuget/dt/Audit.NET.NLog.svg)](https://www.nuget.org/packages/Audit.NET.NLog/)

## Usage
Please see the [Audit.NET Readme](https://github.com/thepirat000/Audit.NET#usage)

## Configuration
To set the NLog data provider globally, call the `UseNLog()` method on the [fluent configuration API](https://github.com/thepirat000/Audit.NET#configuration-fluent-api), for example:

```c#
Audit.Core.Configuration.Setup()
    .UseNLog();
```

You can also configure the provider settings by using the fluent API, for example:
```c#
Audit.Core.Configuration.Setup()
    .UseNLog(config => config
        .Logger(LogManager.GetLogger(typeof(MyClass)))        
        .LogLevel(LogLevel.Debug)
        .Message(auditEvent => auditEvent.ToJson()));
```

The _Logger_ and _LogLevel_ settings can be configured as a function of the audit event, for example:
```c#
Audit.Core.Configuration.Setup()
    .UseNLog(config => config
        .Logger(ev => LogManager.GetLogger(ev.EventType))        
        .LogLevel(ev => ev.Environment.Exception != null ? LogLevel.Fatal : LogLevel.Info));
```

## Settings
- `Logger`: Indicates the NLog [ILogger](https://github.com/nlog/nlog/wiki/Tutorial) logger instance to use. Default is obtained by using the audit event type `LogManager.GetLogger(auditEvent.GetType())`.
- `LogLevel`: Indicates the log level to use (debug, info, warn, error or fatal). Default is _Info_ unless there is an exception, in which case it logs as _Error_.
- `Message`: Indicates the textual message to log. Default is the AuditEvent JSON including the EventId as a custom field.

## ZZZ Projects - Sponsorship

[Entity Framework Extensions](https://entityframework-extensions.net/) and [Dapper Plus](https://dapper-plus.net/) are major sponsors and are proud to contribute to the development of Audit.NET

Combine the power of auditing with the speed of Bulk Operations to get the best of both worlds — audit and performance.

[![Entity Framework Extensions - Sponsor](https://raw.githubusercontent.com/thepirat000/Audit.NET/master/documents/entity-framework-extensions-sponsor.png)](https://entityframework-extensions.net/bulk-insert)

[![Dapper Plus - Sponsor](https://raw.githubusercontent.com/thepirat000/Audit.NET/master/documents/dapper-plus-sponsor.png)](https://dapper-plus.net/bulk-insert)