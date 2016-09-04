# Audit.NET.AzureDocumentDB
**Azure Document DB provider for [Audit.NET library](https://github.com/thepirat000/Audit.NET)**. (An extensible framework to audit executing operations in .NET)

Store the audit events in an Azure Document DB collection, in JSON format.

##Install

**[NuGet Package](https://www.nuget.org/packages/Audit.NET.AzureDocumentDB/)**
```
PM> Install-Package Audit.NET.AzureDocumentDB
```

##Usage
Please see the [Audit.NET Readme](https://github.com/thepirat000/Audit.NET#usage)

##Configuration
Call the static `AuditConfiguration.SetDataProvider` method to set the Document DB data provider, or use the `UseAzureDocumentDB` method on the fluent configuration. This should be done before any `AuditScope` creation, i.e. during application startup.

```c#
AuditConfiguration.SetDataProvider(new Audit.AzureDocumentDB.Providers.AzureDbDataProvider()
{
    ConnectionString = "https://mycompany.documents.azure.com:443/",
    AuthKey = "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx==",
    Database = "Audit",
    Collection = "Event"
});
```

Or by using the [fluent configuration API](https://github.com/thepirat000/Audit.NET#configuration-fluent-api):
```c#
AuditConfiguration.Setup()
    .UseAzureDocumentDB(config => config
        .ConnectionString("https://mycompany.documents.azure.com:443/")
        .AuthKey("xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx==")
        .Database("Audit")
        .Collection("Event"));
```

###Provider options

Mandatory:
- **ConnectionString**: The Azure Document DB Connection String.
- **AuthKey**: The Auth Key to use.
- **Database**: The audit database name.
- **Collection**: The events collection name.

