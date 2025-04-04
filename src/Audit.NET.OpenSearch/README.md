# Audit.NET.OpenSearch
**OpenSearch provider for [Audit.NET library](https://github.com/thepirat000/Audit.NET)** (An extensible framework to audit executing operations in .NET).

Store the audit events in OpenSearch database using the [OpenSearch.Client](https://www.nuget.org/packages/OpenSearch.Client) library.

## Install

**NuGet Package** 
To install the package run the following command on the Package Manager Console:

```
PM> Install-Package Audit.NET.OpenSearch
```

[![NuGet Status](https://img.shields.io/nuget/v/Audit.NET.OpenSearch.svg?style=flat)](https://www.nuget.org/packages/Audit.NET.OpenSearch/)
[![NuGet Count](https://img.shields.io/nuget/dt/Audit.NET.OpenSearch.svg)](https://www.nuget.org/packages/Audit.NET.OpenSearch/)

## Usage
Please see the [Audit.NET Readme](https://github.com/thepirat000/Audit.NET#usage)

## Configuration
Set the static `Audit.Core.Configuration.DataProvider` property to set the OpenSearch data provider, or call the `UseOpenSearch` 
method on the fluent configuration. This should be done before any `AuditScope` creation, i.e. during application startup.


For example:

```c#
Audit.Core.Configuration.DataProvider = new OpenSearchDataProvider()
{
    ClientSettings = new ConnectionSettings(new Uri("http://localhost:9200")),
    Index = new(ev => ev.EventType),
    IdBuilder = ev => Guid.NewGuid()
};
```

Or by using the [fluent configuration API](https://github.com/thepirat000/Audit.NET#configuration-fluent-api):

```c#
Audit.Core.Configuration.Setup()
    .UseOpenSearch(config => config
        .Client(new OpenSearchClient("<CLOUD ID>", new BasicAuthenticationCredentials("user", "pass")));
        .Index(auditEvent => auditEvent.EventType)
        .Id(ev => Guid.NewGuid()));
```

Note that you can provide the settings as a function of the [Audit Event](https://github.com/thepirat000/Audit.NET#usage).

### Provider Options

Mandatory:
- **Client / ClientSettings**: Specifies how to create the OpenSearch client. You can either pass an instance of the OpenSearchClient or provide the OpenSearchClientSettings.

Optional:
- **Index**: The OpenSearch index name to use. Can be NULL to use the default index. 
- **Id / IdBuilder**: The id to use for the given audit event. Can be NULL to use an auto-generated id.

## Query events

This provider implements `GetEvent` and `GetEventAsync` methods to obtain an audit event by id:

```c#
var event = openSearchDataProvider.GetEvent(new OpenSearchAuditEventId() { Index = "myindex", Id = "myid" });
```
