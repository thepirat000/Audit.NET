# Audit.NET.MongoDB
**Mongo DB provider for [Audit.NET library](https://github.com/thepirat000/Audit.NET).** (An extensible framework to audit executing operations in .NET).

Store the audit events in a Mongo DB Collection, in BSON format.

## Install

**NuGet Package** 

```
PM> Install-Package Audit.NET.MongoDB
```

[![NuGet Status](https://img.shields.io/nuget/v/Audit.NET.MongoDB.svg?style=flat)](https://www.nuget.org/packages/Audit.NET.MongoDB/)

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

### Provider options

Mandatory:
- **ConnectionString**: The [Mongo DB connection string](http://mongodb.github.io/mongo-csharp-driver/2.0/reference/driver/connecting/).
- **Database**: The audit Mongo Database name.
- **Collection**: The events Mongo Collection name.

## Output sample

An example of the output as seen with [NoSQL Manager for Mongo DB](http://www.mongodbmanager.com/):

![MongoDB sample](http://i.imgur.com/jyYOypX.png)
