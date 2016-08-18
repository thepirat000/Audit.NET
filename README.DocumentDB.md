# Audit.NET.AzureDocumentDB
Mongo DB provider for [Audit.NET library](https://github.com/thepirat000/Audit.NET) (A small framework to audit executing operations in .NET).

Store the audit events in an Azure Document DB collection, in JSON format.

##Install

**[NuGet Package](https://www.nuget.org/packages/Audit.NET.AzureDocumentDB/)**
```
PM> Install-Package Audit.NET.AzureDocumentDB
```

##Usage
Please see the [Audit.NET Readme](https://github.com/thepirat000/Audit.NET#usage)

##Configuration
Call the static `AuditConfiguration.SetDataProvider` method to set the Document DB data provider. This should be done before any `AuditScope` creation, i.e. during application startup.

```c#
AuditConfiguration.SetDataProvider(new Audit.AzureDocumentDB.Providers.AzureDbDataProvider()
{
    ConnectionString = "https://localhost:443/",
    AuthKey = "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx==",
    Database = "Audit",
    Collection = "Event"
});
```

Provider options:

- **ConnectionString**: The Azure Document DB [connection string](http://mongodb.github.io/mongo-csharp-driver/2.0/reference/driver/connecting/).
- **Database**: The audit database name.
- **Collection**: The events collection name.

