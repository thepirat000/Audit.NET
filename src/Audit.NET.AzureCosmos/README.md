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
Audit.Core.Configuration.DataProvider = new Audit.AzureCosmos.Providers.AzureCosmosDataProvider()
{
    Endpoint = "https://mycompany.documents.azure.com:443/",
    AuthKey = "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx==",
    Database = "Audit",
    ContainerBuilder = () => context.GetContainer(),
    ConnectionPolicy = new ConnectionPolicy { ConnectionMode = ConnectionMode.Direct, ConnectionProtocol = Protocol.Tcp }
};

Audit.Core.Configuration.DataProvider = new Audit.AzureCosmos.Providers.AzureCosmosDataProvider()
{
    DocumentClient = myClient,
    Database = "Audit",
    Container = "Event"
};
```

Or by using the [fluent configuration API](https://github.com/thepirat000/Audit.NET#configuration-fluent-api):
```c#
Audit.Core.Configuration.Setup()
    .UseAzureCosmos(config => config
        .Endpoint("https://mycompany.documents.azure.com:443/")
        .AuthKey("xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx==")
        .Database("Audit")
        .Container("Test")
        .ConnectionPolicy(new ConnectionPolicy
        {
            ConnectionMode = ConnectionMode.Direct,
            ConnectionProtocol = Protocol.Tcp
        }));
```

### Provider options

Mandatory config with an Endpoint and an AuthKey:
- **Endpoint** / **EndpointBuilder**: The Azure Cosmos endpoint URL.
- **AuthKey** / **AuthKeyBuilder**: The Auth Key to use.
- **Database** / **DatabaseBuilder**: The audit database name.
- **Container** / **ContainerBuilder**: The events container name.

Or with a previously configured instance of DocumentClient:
- **DocumentClient**: An already configured document client.

## Query events

This provider implements `GetEvent` and `GetEventAsync` methods to obtain an audit event by id:

```c#
var event = cosmosDbDataProvider.GetEvent("eventId");
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
