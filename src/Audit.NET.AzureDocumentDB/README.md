# Audit.NET.AzureDocumentDB
**Azure Document DB provider for [Audit.NET library](https://github.com/thepirat000/Audit.NET)**. (An extensible framework to audit executing operations in .NET)

Store the audit events in an Azure Document DB collection, in JSON format.

## Install

**NuGet Package** 

```
PM> Install-Package Audit.NET.AzureDocumentDB
```

[![NuGet Status](https://img.shields.io/nuget/v/Audit.NET.AzureDocumentDB.svg?style=flat)](https://www.nuget.org/packages/Audit.NET.AzureDocumentDB/)

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
        .Collection("Event"));
```

### Provider options

Mandatory:
- **ConnectionString**: The Azure Document DB Connection String.
- **AuthKey**: The Auth Key to use.
- **Database**: The audit database name.
- **Collection**: The events collection name.

