# Audit.NET.Elasticsearch
**Elasticsearch provider for [Audit.NET library](https://github.com/thepirat000/Audit.NET)** (An extensible framework to audit executing operations in .NET).

Store the audit events in Elasticsearch database using the [Elastic.Clients.Elasticsearch](https://www.nuget.org/packages/Elastic.Clients.Elasticsearch) library.

## Install

**NuGet Package** 
To install the package run the following command on the Package Manager Console:

```
PM> Install-Package Audit.NET.Elasticsearch
```

[![NuGet Status](https://img.shields.io/nuget/v/Audit.NET.Elasticsearch.svg?style=flat)](https://www.nuget.org/packages/Audit.NET.Elasticsearch/)
[![NuGet Count](https://img.shields.io/nuget/dt/Audit.NET.Elasticsearch.svg)](https://www.nuget.org/packages/Audit.NET.Elasticsearch/)

## Usage
Please see the [Audit.NET Readme](https://github.com/thepirat000/Audit.NET#usage)

## Configuration
Set the static `Audit.Core.Configuration.DataProvider` property to set the Elasticsearch data provider, or call the `UseElasticsearch` 
method on the fluent configuration. This should be done before any `AuditScope` creation, i.e. during application startup.


For example:

```c#
Audit.Core.Configuration.DataProvider = new ElasticsearchDataProvider()
{
    ClientSettings = new ElasticsearchClientSettings(new Uri("http://localhost:9200")),
    Index = new(ev => ev.EventType),
    IdBuilder = ev => Guid.NewGuid()
};
```

Or by using the [fluent configuration API](https://github.com/thepirat000/Audit.NET#configuration-fluent-api):

```c#
Audit.Core.Configuration.Setup()
    .UseElasticsearch(config => config
        .Client(new ElasticsearchClient("<CLOUD ID>", new BasicAuthentication("user", "pass")))
        .Index(auditEvent => auditEvent.EventType)
        .Id(ev => Guid.NewGuid()));
```

Note that you can provide the settings as a function of the [Audit Event](https://github.com/thepirat000/Audit.NET#usage).


### Provider Options

Mandatory:
- **Client / ClientSettings**: Specifies how to create the Elasticsearch client. You can either pass an instance of the ElasticsearchClient or provide the ElasticsearchClientSettings.

Optional:
- **Index**: The Elasticsearch index name to use. Can be NULL to use the default index. 
- **Id / IdBuilder**: The id to use for the given audit event. Can be NULL to use an auto-generated id.

## Query events

This provider implements `GetEvent` and `GetEventAsync` methods to obtain an audit event by id:

```c#
var event = elasDataProvider.GetEvent(new ElasticsearchAuditEventId() { Index = "myindex", Id = "myid" });
```
