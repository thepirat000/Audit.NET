# Audit.NET.log4net
**log4net storage provider for [Audit.NET library](https://github.com/thepirat000/Audit.NET)** (An extensible framework to audit executing operations in .NET).

Store the audit events using [Apache log4net™](https://logging.apache.org/log4net/).

## Install

**NuGet Package** 

To install the package, run the following command on the Package Manager Console:

```
PM> Install-Package Audit.NET.log4net
```

[![NuGet Status](https://img.shields.io/nuget/v/Audit.NET.log4net.svg?style=flat)](https://www.nuget.org/packages/Audit.NET.log4net/)
[![NuGet Count](https://img.shields.io/nuget/dt/Audit.NET.log4net.svg)](https://www.nuget.org/packages/Audit.NET.log4net/)

## Usage
Please see the [Audit.NET Readme](https://github.com/thepirat000/Audit.NET#usage)

## Configuration
To set the log4net data provider globally, call the `UseLog4net()` method on the [fluent configuration API](https://github.com/thepirat000/Audit.NET#configuration-fluent-api), for example:

```c#
Audit.Core.Configuration.Setup()
    .UseLog4net();
```

You can also configure the provider settings by using the fluent API, for example:
```c#
Audit.Core.Configuration.Setup()
    .UseLog4net(config => config
        .Logger(LogManager.GetLogger(typeof(MyClass)))        
        .LogLevel(LogLevel.Debug)
        .Message(auditEvent => auditEvent.ToJson()));
```

The _Logger_ and _LogLevel_ settings can be configured as a function of the audit event, for example:
```c#
Audit.Core.Configuration.Setup()
    .UseLog4net(config => config
        .Logger(ev => LogManager.GetLogger(ev.EventType))        
        .LogLevel(ev => ev.Environment.Exception != null ? LogLevel.Fatal : LogLevel.Info));
```

## Settings
- `Logger` Indicates the log4net [ILog](https://logging.apache.org/log4net/release/manual/introduction.html) logger instance to use. Default is obtained by using the audit event type `LogManager.GetLogger(auditEvent.GetType())`.
- `LogLevel` Indicates the log level to use (debug, info, warn, error or fatal). Default is _Info_ unless there is an exception, in which case it logs as _Error_.
- `Message` Indicates the textual message to log. Default is the AuditEvent JSON including the EventId as a custom field.
