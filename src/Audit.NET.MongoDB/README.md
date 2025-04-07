# Audit.NET.MongoDB
**Mongo DB provider for [Audit.NET library](https://github.com/thepirat000/Audit.NET).** (An extensible framework to audit executing operations in .NET).

Store the audit events in a Mongo DB Collection, in BSON format.

## Install

**NuGet Package** 

```
PM> Install-Package Audit.NET.MongoDB
```

[![NuGet Status](https://img.shields.io/nuget/v/Audit.NET.MongoDB.svg?style=flat)](https://www.nuget.org/packages/Audit.NET.MongoDB/)
[![NuGet Count](https://img.shields.io/nuget/dt/Audit.NET.MongoDB.svg)](https://www.nuget.org/packages/Audit.NET.MongoDB/)

## Usage
Please see the [Audit.NET Readme](https://github.com/thepirat000/Audit.NET#usage)

## Configuration
Set the static `Audit.Core.Configuration.DataProvider` property to set the Mongo DB data provider, or call the `UseMongoDB` method on the fluent configuration. This should be done before any `AuditScope` creation, i.e. during application startup.

For example:
```c#
Audit.Core.Configuration.DataProvider = new Audit.MongoDB.Providers.MongoDataProvider()
{
    ConnectionString = "mongodb://localhost:27017",
    Database = "Audit",
    Collection = "Event"
};
```

Or by using the [fluent configuration API](https://github.com/thepirat000/Audit.NET#configuration-fluent-api):
```c#
Audit.Core.Configuration.Setup()
    .UseMongoDB(config => config
        .ConnectionString("mongodb://localhost:27017")
        .Database("Audit")
        .Collection("Event"));
```

Instead of using the connection string, you have the option to supply the `MongoClientSettings` instead:

```c#
Audit.Core.Configuration.Setup()
    .UseMongoDB(config => config
        .ClientSettings(new MongoClientSettings() 
        { 
            Server = new MongoServerAddress("localhost", 27017), 
            UseTls = true 
        })
        .Database("Audit")
        .Collection("Event"));
```

You can find more information about MongoClientSettings [here](https://www.mongodb.com/docs/drivers/csharp/current/fundamentals/connection/connection-options/#using-mongoclientsettings).


### Provider options

Mandatory:
- **ConnectionString**: The [Mongo DB connection string](http://mongodb.github.io/mongo-csharp-driver/2.0/reference/driver/connecting/).
- **ClientSettings**: The [Mongo DB client settings](https://www.mongodb.com/docs/drivers/csharp/current/fundamentals/connection/connection-options/#using-mongoclientsettings).
- **Database**: The audit Mongo Database name.
- **DatabaseSettings**: The audit Mongo Database settings (optional).
- **Collection**: The events Mongo Collection name.
- **SerializeAsBson**: A value indicating whether the target object and extra fields should be serialized as Bson. Default is false to serialize as Json.

## Output sample

An example of the output as seen with [NoSQL Manager for Mongo DB](http://www.mongodbmanager.com/):

![MongoDB sample](http://i.imgur.com/jyYOypX.png)

## Query events

This provider implements `GetEvent` and `GetEventAsync` methods to obtain an audit event by id:

```c#
var event = mongoDataProvider.GetEvent("57b60f25be5914a355771629");
```

The Mongo DB data provider also includes support for querying the events collection.

You can use the `QueryEvents()` method on `MongoDataProvider` class to run LINQ queries against the audit events.

For example, to get the top 10 most time-consuming events for a specific machine:
```c#
IQueryable<AuditEvent> query = mongoDataProvider.QueryEvents()
	.Where(ev => ev.Environment.MachineName == "HP")
	.OrderByDescending(ev => ev.Duration)
	.Take(10);
```

## ZZZ Projects - Sponsorship

[Entity Framework Extensions](https://entityframework-extensions.net/) and [Dapper Plus](https://dapper-plus.net/) are major sponsors and are proud to contribute to the development of Audit.NET

Combine the power of auditing with the speed of Bulk Operations to get the best of both worlds — audit and performance.

<a href="https://entityframework-extensions.net/bulk-insert"><img width="728" height="90" alt="Entity Framework Extensions - Sponsor" src="https://raw.githubusercontent.com/JonathanMagnan/Audit.NET/master/entity-framework-extensions-sponsor.png" /></a>

<a href="https://dapper-plus.net/bulk-insert"><img width="728" height="90" alt="Dapper Plus - Sponsor" src="https://raw.githubusercontent.com/JonathanMagnan/Audit.NET/master/dapper-plus-sponsor.png" /></a>
