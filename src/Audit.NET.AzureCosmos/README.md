# Audit.NET.AzureCosmos
**Azure Cosmos DB (Document DB SQL API) provider for [Audit.NET library](https://github.com/thepirat000/Audit.NET)**. (An extensible framework to audit executing operations in .NET)

Store the audit events in an Azure Cosmos DB collection, in JSON format.

## Install

**NuGet Package** 
To install the package run the following command on the Package Manager Console:

```
PM> Install-Package Audit.NET.AzureCosmos
```

[![NuGet Status](https://img.shields.io/nuget/v/Audit.NET.AzureCosmos.svg?style=flat)](https://www.nuget.org/packages/Audit.NET.AzureCosmos/)
[![NuGet Count](https://img.shields.io/nuget/dt/Audit.NET.AzureCosmos.svg)](https://www.nuget.org/packages/Audit.NET.AzureCosmos/)

## Usage
Please see the [Audit.NET Readme](https://github.com/thepirat000/Audit.NET#usage)

## Configuration
Set the static `Audit.Core.Configuration.DataProvider` property to set the Cosmos DB data provider, 
or use the `UseAzureCosmos` method on the fluent configuration. This should be done before any `AuditScope` creation, i.e. during application startup.

For example:
```c#
Audit.Core.Configuration.DataProvider = new Audit.AzureCosmos.Providers.AzureCosmosDataProvider(config => config
    .Endpoint("https://mycompany.documents.azure.com:443/")
    .AuthKey("xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx==")
    .Database("Audit")
    .Container("logs")
    .WithId(_ => Guid.NewGuid().ToString().ToUpper()));
```

Or by using the [fluent configuration API](https://github.com/thepirat000/Audit.NET#configuration-fluent-api):
```c#
Audit.Core.Configuration.Setup()
    .UseAzureCosmos(config => config
        .Endpoint("https://mycompany.documents.azure.com:443/")
        .AuthKey("xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx==")
        .Database("Audit")
        .Container("logs")
        .ClientOptions(options => { options.ConnectionMode = ConnectionMode.Gateway; }));
```

### Provider options

Mandatory config with an Endpoint and an AuthKey:
- **Endpoint** / **EndpointBuilder**: The Azure Cosmos endpoint URL.
- **AuthKey** / **AuthKeyBuilder**: The Auth Key to use.

Or with a previously configured instance of DocumentClient/CosmosClient:
- **DocumentClient** / **CosmosClient**: Sets an already configured document/cosmos client. 

Container settings:
- **Database** / **DatabaseBuilder**: The audit database name.
- **Container** / **ContainerBuilder**: The events container name.
- **WithId** / **IdBuilder**: A func that returns the document id to use for a given audit event. By default it will generate a new random id.

## Query events

This provider implements `GetEvent` and `GetEventAsync` methods to obtain an audit event by id and partition key. Note that if your container
has a partition key defined, you need to provide both id and partition key:

```c#
var event = auditDataProvider.GetEvent(("eventId", "partitionValue"));
```

or using the overload on the concrete AzureCosmosDataProvider:

```c#
var event = azureCosmosDataProvider.GetEvent("eventId", "partitionValue");
```

The Azure Cosmos data provider also includes support for querying the events collection.

Use the `QueryEvents()` method on `AzureCosmosDataProvider` class to run LINQ queries against the audit events.

For example, to get the top 10 most time-consuming events for a specific machine:
```c#
IQueryable<AuditEvent> query = azureCosmosDataProvider.QueryEvents()
	.Where(ev => ev.Environment.MachineName == "HP")
	.OrderByDescending(ev => ev.Duration)
	.Take(10);
```

Also you can use the `EnumerateEvents()` method to run SQL-like queries. For example the previous query can be written as:

```c#
IEnumerable<AuditEvent> events = cosmosDbDataProvider.EnumerateEvents(
       @"SELECT TOP 10 * 
         FROM c 
         WHERE c.Environment.MachineName = 'HP' 
         ORDER BY c.Duration DESC");
```

This [post](https://docs.microsoft.com/en-us/azure/documentdb/documentdb-sql-query) contains information about the SQL query syntax supported by Azure Document DB.

## ZZZ Projects - Sponsorship

[Entity Framework Extensions](https://entityframework-extensions.net/) and [Dapper Plus](https://dapper-plus.net/) are major sponsors and are proud to contribute to the development of Audit.NET

Combine the power of auditing with the speed of Bulk Operations to get the best of both worlds — audit and performance.

[![Entity Framework Extensions - Sponsor](https://raw.githubusercontent.com/thepirat000/Audit.NET/master/documents/entity-framework-extensions-sponsor.png)](https://entityframework-extensions.net/bulk-insert)

[![Dapper Plus - Sponsor](https://raw.githubusercontent.com/thepirat000/Audit.NET/master/documents/dapper-plus-sponsor.png)](https://dapper-plus.net/bulk-insert)