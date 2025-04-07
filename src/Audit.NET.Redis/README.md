# Audit.NET.Redis
**Redis storage provider for [Audit.NET library](https://github.com/thepirat000/Audit.NET)** (An extensible framework to audit executing operations in .NET).

Store the audit events in a Redis database as Strings, List, SortedSet, Hash, publish to a PubSub channel or add to a Redis Stream.

## Install

**NuGet Package** 
To install the package run the following command on the Package Manager Console:

```
PM> Install-Package Audit.NET.Redis
```

[![NuGet Status](https://img.shields.io/nuget/v/Audit.NET.Redis.svg?style=flat)](https://www.nuget.org/packages/Audit.NET.Redis/)
[![NuGet Count](https://img.shields.io/nuget/dt/Audit.NET.Redis.svg)](https://www.nuget.org/packages/Audit.NET.Redis/)

## Usage
Please see the [Audit.NET Readme](https://github.com/thepirat000/Audit.NET#usage)

## Configuration
To set the Redis data provider globally, use the [fluent configuration API](https://github.com/thepirat000/Audit.NET#configuration-fluent-api), for example:
```c#
Audit.Core.Configuration.Setup()
    .UseRedis(redis => redis
        .ConnectionString(RedisCnnString)
        .AsString(str => str
            .Key(ev => $"{Guid.NewGuid()}")));
```

If you need to set different providers/configuration per scope, you can use the `RedisDataProviderHelper` to easily get a new Redis Data Provider via the fluent API, for example:
```c#
    var dp = new RedisDataProviderHelper(RedisCnnString)
        .AsString(str => str
            .Key(ev => $"{Guid.NewGuid()}"));

    using (AuditScope.Create(new AuditScopeOptions() { DataProvider = dp }))
    {
       // ...
    }
```

## Common settings
- `ConnectionString` indicates the redis connection string, based on [StackExchange.Redis configuration](https://stackexchange.github.io/StackExchange.Redis/Configuration).
- `ConfigurationOptions` indicates the redis connection configuration options, when you need to provide custom connection options. Alternative to ConnectionString.
- `Serializer` indicates a custom serialization method for the audit events. By default the events are serialized as JSON encoded as UTF-8.

## Modes

This data provider allows to store the events in different ways.

### Redis String

Store each audit event as a redis string.

Setup sample:
```c#
// Store audits as strings.
Audit.Core.Configuration.Setup()
    .UseRedis(redis => redis
        .ConnectionString("localhost:6379,allowAdmin=true")
        .AsString(str => str
            .Key(ev => $"{ev.EventType}:{Guid.NewGuid()}")));
```

Mandatory settings
- Use the `Key` method to indicate the redis key where the event will be stored, this key can depend on the `AuditEvent` object.

Optional settings
- `TimeToLive` specifies the Time To Live for the Redis Key. Default is no TTL.
- `Database` specifies the database ID to use. Default is database 0.
- `AttachTask` attaches an additional redis command to the execution.

### Redis Stream

Adds each audit event to a Redis Stream.

Setup sample:
```c#
// Store audits as strings.
Audit.Core.Configuration.Setup()
    .UseRedis(redis => redis
        .ConnectionString("localhost")
        .AsStream(stream => stream
            .Key("MyAuditStream")
            .MaxLength(5000)
            .WithCustomField("Date", ev => DateTime.UtcNow.ToString())));
```

Mandatory settings
- Use the `Key` method to indicate the stream redis key, this key can depend on the `AuditEvent` object.

Optional settings
- `Database` specifies the database ID to use. Default is database 0.
- `AttachTask` attaches an additional redis command to the execution.
- `MaxLength` specifies the maximum quantity of events that the stream will store. Older elements will be deleted. Default is NULL for no-limit.
- `DefaultAuditEventFieldName` specifies the default field name that will contain the AuditEvent JSON representation in the stream entry. Default is "AuditEvent".
- `WithCustomField` allows specifying custom fields to be stored in the stream entry. By default, only the field named "AuditEvent" is stored, containing the JSON representation of the Audit Event.

### Redis Hash

Store each audit event as a field in a redis hash.

Setup sample:
```c#
// Store audits in hashes per each EventType.
Audit.Core.Configuration.Setup()
    .UseRedis(redis => redis
        .ConnectionString(RedisCnnString)
        .AsHash(hash => hash
            .Key(ev => $"{ev.EventType}")
            .HashField(ev => $"{Guid.NewGuid()}")));
```

Mandatory settings:
- `Key` to indicate the redis key where the hash will be stored, this key can depend on the `AuditEvent` object.
- `HashField` to indicate the redis field inside the hash where the event will be stored.

Optional settings
- `TimeToLive` specifies the Time To Live for the Redis Hash. Default is no TTL.
- `Database` specifies the database ID to use. Default is database 0.
- `AttachTask` attaches an additional redis command to the execution.

### Redis List

Store each audit event as a member in a redis list.

Setup sample:
```c#
// Store audits in lists per each EventType, with a maximum of 1000 events per list.
Audit.Core.Configuration.Setup()
    .UseRedis(redis => redis
        .ConnectionString(RedisCnnString)
        .AsList(list => list
            .Key(ev => $"{ev.EventType}")
            .MaxLength(1000)));
```

Mandatory settings:
- `Key` to indicate the redis key where the list will be stored, this key can depend on the `AuditEvent` object.

Optional settings:
- `MaxLength` to indicate the maximum quantity of events that the list will store. Older elements will be deleted. Default is no-limit.
- `TimeToLive` specifies the Time To Live for the Redis List. Default is no TTL.
- `Database` specifies the database ID to use. Default is database 0.
- `AttachTask` attaches an additional redis command to the execution.

### Redis Sorted Set

Store each audit event as a member in a redis sorted set.

Setup sample:
```c#
// Store audits in sorted sets per each EventType, maintaining only the events from the last 30 days.
Audit.Core.Configuration.Setup()
    .UseRedis(redis => redis
        .ConnectionString(RedisCnnString)
        .AsSortedSet(sset => sset
            .Key(ev => $"{ev.EventType}")
            .Score(ev => ev.StartDate.Ticks)
            .MinScore(ev => DateTime.UtcNow.AddDays(-30).Ticks)));
```

Mandatory settings:
- `Key` to indicate the redis key where the sorted set will be stored, this key can depend on the `AuditEvent` object.
- `Score` to indicate the score as a `double` that the event will have, can depend on the `AuditEvent` object.

Optional settings:
- `MinScore` specifies a function that returns the minimum score allowed for the sorted set. Audits with score less than the minimum will be deleted. This deletion takes place when a new event is stored.
- `MaxScore` specifies a function that returns the maximum score allowed for the sorted set.
- `MaxRank` specifies a function that returns the maximum rank allowed for the sorted set. Max>0: Maintain only the top N scored elements.
Max<0: Maintain only the bottom N scored elements.
- `TimeToLive` specifies the Time To Live for the Redis Sorted Set. Default is no TTL.
- `Database` specifies the database ID to use. Default is database 0.
- `AttachTask` attaches an additional redis command to the execution.

### Redis PubSub

Sends the audit events to a redis PubSub channel.

Setup sample:
```c#
// Send audits to PubSub channels.
Audit.Core.Configuration.Setup()
    .UseRedis(redis => redis
        .ConnectionString(RedisCnnString)
        .AsPubSub(pub => pub
            .Channel(ev => $"audits:{ev.EventType}")));
```

Mandatory settings:
- `Channel` to indicate the channel name to use for publishing the events.

## Query events

This provider implements `GetEvent` and `GetEventAsync` methods to obtain an audit event by id:

```c#
var event = Configuration.DataProvider.GetEvent("eventId");
```

## ZZZ Projects - Sponsorship

[Entity Framework Extensions](https://entityframework-extensions.net/) and [Dapper Plus](https://dapper-plus.net/) are major sponsors and are proud to contribute to the development of Audit.NET

Combine the power of auditing with the speed of Bulk Operations to get the best of both worlds — audit and performance.

<a href="https://entityframework-extensions.net/bulk-insert"><img width="728" height="90" alt="Entity Framework Extensions - Sponsor" src="https://raw.githubusercontent.com/JonathanMagnan/Audit.NET/master/entity-framework-extensions-sponsor.png" /></a>

<a href="https://dapper-plus.net/bulk-insert"><img width="728" height="90" alt="Dapper Plus - Sponsor" src="https://raw.githubusercontent.com/JonathanMagnan/Audit.NET/master/dapper-plus-sponsor.png" /></a>
