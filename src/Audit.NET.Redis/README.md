# Audit.NET.Redis
**Redis storage provider for [Audit.NET library](https://github.com/thepirat000/Audit.NET)** (An extensible framework to audit executing operations in .NET).

Store the audit events in a Redis database as Strings, List, SortedSet, Hash or publish to a PubSub channel.

## Install

**NuGet Package** 
To install the package run the following command on the Package Manager Console:

```
PM> Install-Package Audit.NET.Redis
```

[![NuGet Status](https://img.shields.io/nuget/v/Audit.NET.SqlServer.svg?style=flat)](https://www.nuget.org/packages/Audit.NET.Redis/)

## Usage
Please see the [Audit.NET Readme](https://github.com/thepirat000/Audit.NET#usage)

## Configuration
To set the Redis data provider globally, use the [fluent configuration API](https://github.com/thepirat000/Audit.NET#configuration-fluent-api):

For example:
```c#
Audit.Core.Configuration.Setup()
   blahblah
```

If you need to set different providers/configuration per scope, you can use the `RedisDataProviderHelper` to get a new data provider using the same fluent API:
```c#
AuditScope.Create(blabla)
   blahblah
```


