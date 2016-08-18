# Audit.NET.MongoDB
Mongo DB provider for [Audit.NET library](https://github.com/thepirat000/Audit.NET) (A small framework to audit executing operations in .NET).

Store the audit events in a Mongo DB Collection, in BSON format.

##Install

**[NuGet Package](https://www.nuget.org/packages/Audit.NET.MongoDB/)**
```
PM> Install-Package Audit.NET.MongoDB
```

##Usage
Please see the [Audit.NET Readme](https://github.com/thepirat000/Audit.NET#usage)

##Configuration
Call the static `AuditConfiguration.SetDataProvider` method to set the Mongo DB data provider. This should be done before any `AuditScope` creation, i.e. during application startup.

```c#
AuditConfiguration.SetDataProvider(new Audit.MongoDB.Providers.MongoDataProvider()
{
    ConnectionString = "mongodb://localhost:27017",
    Database = "Audit",
    Collection = "Event",
    ShouldTestConnection = true
});
```

Provider options:

- **ConnectionString**: The [Mongo DB connection string](http://mongodb.github.io/mongo-csharp-driver/2.0/reference/driver/connecting/).
- **Database**: The audit database name.
- **Collection**: The events collection name.
- **ShouldTestConnection**: Set to true to indicate the connection to Mongo DB should be tested to allow the audited operation.

An example of the output as seen with [NoSQL Manager for Mongo DB](http://www.mongodbmanager.com/):

![MongoDB sample](http://i.imgur.com/jyYOypX.png)
