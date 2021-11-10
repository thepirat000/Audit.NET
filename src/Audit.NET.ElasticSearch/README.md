# Audit.NET.Elasticsearch
**Elasticsearch provider for [Audit.NET library](https://github.com/thepirat000/Audit.NET)** (An extensible framework to audit executing operations in .NET).

Store the audit events in Elasticsearch database using the [NEST](https://www.nuget.org/packages/Nest) library.

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
    ConnectionSettings = new AuditConnectionSettings(new Uri("http://localhost:9200")),
    IndexBuilder = ev => ev.EventType,
    IdBuilder = ev => Guid.NewGuid()
};
```

Or by using the [fluent configuration API](https://github.com/thepirat000/Audit.NET#configuration-fluent-api):
```c#
Audit.Core.Configuration.Setup()
    .UseElasticsearch(config => config
        .ConnectionSettings(new Uri("http://localhost:9200"))
        .Index(auditEvent => auditEvent.EventType)
        .Id(ev => Guid.NewGuid()));
```

Note that you can provide the settings as a function of the [Audit Event](https://github.com/thepirat000/Audit.NET#usage).


### Provider Options

Mandatory:
- **ConnectionSettings**: The Elasticsearch connection settings. It is recommended to be an instance of `AuditConnectionSettings`.

Optional:
- **Index**: The Elasticsearch index name to use. Can be NULL to use the default index. 
- **Id**: The id to use for the given audit event. Can be NULL to use an auto-generated id.

## Query events

This provider implements `GetEvent` and `GetEventAsync` methods to obtain an audit event by id:

```c#
var event = elasDataProvider.GetEvent(new ElasticsearchAuditEventId() { Index = "myindex", Id = "myid" });
```
