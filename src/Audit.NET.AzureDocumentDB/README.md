# Audit.NET.AzureDocumentDB
**Azure Document DB provider for [Audit.NET library](https://github.com/thepirat000/Audit.NET)**. (An extensible framework to audit executing operations in .NET)

Store the audit events in an Azure Document DB collection, in JSON format.

## Install

**NuGet Package** 
To install the package run the following command on the Package Manager Console:

```
PM> Install-Package Audit.NET.AzureDocumentDB
```

[![NuGet Status](https://img.shields.io/nuget/v/Audit.NET.AzureDocumentDB.svg?style=flat)](https://www.nuget.org/packages/Audit.NET.AzureDocumentDB/)
[![NuGet Count](https://img.shields.io/nuget/dt/Audit.NET.AzureDocumentDB.svg)](https://www.nuget.org/packages/Audit.NET.AzureDocumentDB/)

## Usage
Please see the [Audit.NET Readme](https://github.com/thepirat000/Audit.NET#usage)

## Configuration
Set the static `Audit.Core.Configuration.DataProvider` property to set the Document DB data provider, or use the `UseAzureDocumentDB` method on the fluent configuration. This should be done before any `AuditScope` creation, i.e. during application startup.

For example:
```c#
Audit.Core.Configuration.DataProvider = new Audit.AzureDocumentDB.Providers.AzureDbDataProvider()
{
    ConnectionString = "https://mycompany.documents.azure.com:443/",
    AuthKey = "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx==",
    Database = "Audit",
    CollectionBuilder = auditEvent => auditEvent.EventType,
    ConnectionPolicy = new ConnectionPolicy { ConnectionMode = ConnectionMode.Direct, ConnectionProtocol = Protocol.Tcp }
};

Audit.Core.Configuration.DataProvider = new Audit.AzureDocumentDB.Providers.AzureDbDataProvider()
{
    DocumentClient = myClient,
    DatabaseBuilder = "Audit",
    Collection = "Event"
};
```

Or by using the [fluent configuration API](https://github.com/thepirat000/Audit.NET#configuration-fluent-api):
```c#
Audit.Core.Configuration.Setup()
    .UseAzureDocumentDB(config => config
        .ConnectionString("https://mycompany.documents.azure.com:443/")
        .AuthKey("xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx==")
        .Database("Audit")
        .Collection(auditEvent => auditEvent.EventType)
        .ConnectionPolicy(new ConnectionPolicy
        {
            ConnectionMode = ConnectionMode.Direct,
            ConnectionProtocol = Protocol.Tcp
        }));

Audit.Core.Configuration.Setup()
    .UseAzureDocumentDB(config => config
        .DocumentClient(myClient)
        .Database("Audit")
        .CollectionBuilder = auditEvent => auditEvent.EventType.StartsWith("GET") ? "Web" : "Custom"
);
```

### Provider options

Mandatory config with a ConnectionString and an AuthKey:
- **ConnectionString** / **ConnectionStringBuilder**: The Azure Document DB Connection String.
- **AuthKey** / **AuthKeyBuilder**: The Auth Key to use.
- **Database** / **DatabaseBuilder**: The audit database name.
- **Collection** / **CollectionBuilder**: The events collection name.

Or with a previously configured instance of DocumentClient:
- **DocumentClient**: An already configured document client.

## Query events

This provider implements `GetEvent` and `GetEventAsync` methods to obtain an audit event by id:

```c#
var event = azureDbDataProvider.GetEvent("eventId");
```

The Azure Document DB data provider also includes support for querying the events collection.

Use the `QueryEvents()` method on `AzureDbDataProvider` class to run LINQ queries against the audit events.


For example, to get the top 10 most time-consuming events for a specific machine:
```c#
IQueryable<AuditEvent> query = azureDbDataProvider.QueryEvents()
	.Where(ev => ev.Environment.MachineName == "HP")
	.OrderByDescending(ev => ev.Duration)
	.Take(10);
```

Also you can use the `EnumerateEvents()` method to run SQL-like queries. For example the previous query can be written as:

```c#
IEnumerable<AuditEvent> events = azureDbDataProvider.EnumerateEvents(
       @"SELECT TOP 10 * 
         FROM c 
         WHERE c.Environment.MachineName = 'HP' 
         ORDER BY c.Duration DESC");
```

This [post](https://docs.microsoft.com/en-us/azure/documentdb/documentdb-sql-query) contains information about the SQL query syntax supported by Azure Document DB.
