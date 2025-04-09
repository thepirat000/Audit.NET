# Audit.NET.Channels
**Channel provider for [Audit.NET library](https://github.com/thepirat000/Audit.NET)** (An extensible framework to audit executing operations in .NET).

Store the audit events in memory in a bounded or unbounded Channel ([System.Threading.Channels](https://learn.microsoft.com/en-us/dotnet/core/extensions/channels)).

Audit events are produced to the channel for a consumer to retrieve.



## Install

**NuGet Package** 
To install the package run the following command on the Package Manager Console:

```
PM> Install-Package Audit.NET.Channels
```

[![NuGet Status](https://img.shields.io/nuget/v/Audit.NET.Channels.svg?style=flat)](https://www.nuget.org/packages/Audit.NET.Channels/)
[![NuGet Count](https://img.shields.io/nuget/dt/Audit.NET.Channels.svg)](https://www.nuget.org/packages/Audit.NET.Channels/)

## Usage
Please see the [Audit.NET Readme](https://github.com/thepirat000/Audit.NET#usage)

## Configuration
Set the static `Audit.Core.Configuration.DataProvider` property to set the Channel data provider, or call the `UseInMemoryChannel` method on the fluent configuration. 
This should be done before any `AuditScope` creation, i.e. during application startup.

For example:

```c#
Audit.Core.Configuration.DataProvider = new ChannelDataProvider(Channel.CreateUnbounded<AuditEvent>());
```

Or by using the [fluent configuration API](https://github.com/thepirat000/Audit.NET#configuration-fluent-api):

```c#
Audit.Core.Configuration.Setup()
    .UseInMemoryChannelProvider(channel => channel.Bounded(100));
```

### Provider Options

- **Bounded**: Set the maximum number of events that the channel can hold. When the channel is full, the producer will be blocked until the consumer retrieves some events from the channel.
- **Unbounded**: The channel can hold an unlimited number of events. The producer will never be blocked.

## Consume events

The `ChannelDataProvider` allows to consume the events by providing a `TakeAsync` and `TryTakeAsync` methods.

For example:

```c#
// Start up
Audit.Core.Configuration.DataProvider = new ChannelDataProvider(c => c.Unbounded());

// Consumer
var dataProvider = Audit.Core.Configuration.DataProviderAs<ChannelDataProvider>();
while (!cancellationToken.IsCancellationRequested)
{
    var auditEvent = await dataProvider.TakeAsync(cancellationToken);

    // Handle the auditEvent...
    Handle(auditEvent);
}

```

## ZZZ Projects - Sponsorship

[Entity Framework Extensions](https://entityframework-extensions.net/) and [Dapper Plus](https://dapper-plus.net/) are major sponsors and are proud to contribute to the development of Audit.NET

Combine the power of auditing with the speed of Bulk Operations to get the best of both worlds — audit and performance.

[![Entity Framework Extensions - Sponsor](https://raw.githubusercontent.com/thepirat000/Audit.NET/master/documents/entity-framework-extensions-sponsor.png)](https://entityframework-extensions.net/bulk-insert)

[![Dapper Plus - Sponsor](https://raw.githubusercontent.com/thepirat000/Audit.NET/master/documents/dapper-plus-sponsor.png)](https://dapper-plus.net/bulk-insert)