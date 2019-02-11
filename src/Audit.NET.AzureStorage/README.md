# Audit.NET.AzureStorage
**Azure Blob** and **Azure Table** storage providers for [Audit.NET library](https://github.com/thepirat000/Audit.NET) (An extensible framework to audit executing operations in .NET).

Store the audit events in an Azure Storage container, as Blob files in JSON format, or as rows on an Azure Table.

## Install

**NuGet Package** 
To install the package run the following command on the Package Manager Console:

```
PM> Install-Package Audit.NET.AzureStorage
```

[![NuGet Status](https://img.shields.io/nuget/v/Audit.NET.AzureStorage.svg?style=flat)](https://www.nuget.org/packages/Audit.NET.AzureStorage/)
[![NuGet Count](https://img.shields.io/nuget/dt/Audit.NET.AzureStorage.svg)](https://www.nuget.org/packages/Audit.NET.AzureStorage/)

## Usage
Please see the [Audit.NET Readme](https://github.com/thepirat000/Audit.NET#usage)

## Configuration
Set the static `Audit.Core.Configuration.DataProvider` property to set one of the Azure Storage data providers, or call the `UseAzureBlobStorage()`/`UseAzureTableStorage()` 
methods on the fluent configuration. This should be done before any `AuditScope` creation, i.e. during application startup.

### 1- Azure Blob Storage

Stores each audit event as a BLOB file with the JSON representation of the Audit Event.

#### Configuration examples

Using a connection string: 
 
```c#
Audit.Core.Configuration.Setup()
    .UseAzureBlobStorage(config => config
        .ConnectionString("DefaultEndpointsProtocol=https;AccountName=your account;AccountKey=your key")
        .ContainerName("event")
        .BlobNameBuilder(ev => $"{ev.StartDate:yyyy-MM}/{ev.Environment.UserName}/{Guid.NewGuid()}.json"));
```

Using Azure Active Directory (authentication token):

```c#
Audit.Core.Configuration.Setup()
    .UseAzureBlobStorage(config => config
        .UseAzureActiveDirectory(adConfig => adConfig
            .AccountName("your account")
            .TenantId("your tenant ID"))
        .ContainerName("event")
        .BlobNameBuilder(ev => $"{Guid.NewGuid()}.json"));
```

#### Provider Options

##### Authentication options

Depending on the authentication method, you can call one of the following two methods:

- **`ConnectionString()`**: To use **Account Access Key or SAS** authentication via an Azure Storage connection string.
- **`AzureActiveDirectory()`**: To use [**Azure Active Directory**](https://docs.microsoft.com/en-us/azure/storage/common/storage-auth-aad-app) authentication via access tokens acquired automatically.

##### AzureActiveDirectory configuration

When using Azure Active Directory authentication you can provide the following configuration to `AzureActiveDirectory()` method:

- **`AccountName()`**: The Account Name of your Azure Storage.
- **`TenantId()`**: The Tenant ID, this is your Azure Active Directory ID. See [How to get it](https://docs.microsoft.com/en-us/azure/storage/common/storage-auth-aad-app#get-the-tenant-id-for-your-azure-active-directory).

- **`AuthConnectionString()`**: (Optional) Specifies a custom connection string for the Token Provider. Check [this](https://docs.microsoft.com/en-us/azure/key-vault/service-to-service-authentication#connection-string-support) for more information.
- **`ResouceUrl()`**: (Optional) Specifies a custom resource URL to acquire the tokens for. By default the Azure Storage resource ID `https://storage.azure.com/` is used.
- **`EndpointSuffix()`**: (Optional) Specifies a custom DNS endpoint suffix to use for the storage services. Default is `core.windows.net`.
- **`UseHttps()`**: (Optional) Specifies whether to use HTTPS to connect to storage service endpoints. Default is `true`.

##### Container options

- **`ContainerName()`/`ContainerNameBuilder`**: The container name to use (see the naming restrictions [here](https://docs.microsoft.com/en-us/rest/api/storageservices/naming-and-referencing-containers--blobs--and-metadata)). By default, "event" is used as the container name.
- **`BlobNameBuilder`**: A function that takes an Audit Event and returns a unique blob name for the event. The resulting name can include path information (slash separated sub-folders). By default, a random Guid is used as the blob name.

#### Query events

This provider implements `GetEvent` and `GetEventAsync` methods to obtain an audit event by id:

```c#
var event = blobDataProvider.GetEvent("eventId");
```

### 2- Azure Table Storage

Stores each audit event as a new row on an Azure Table, allowing to dynamically configure the columns.

Configuration example:
```c#
Audit.Core.Configuration.DataProvider = new AzureTableDataProvider()
{
    ConnectionString = "DefaultEndpointsProtocol=https;AccountName=your account;AccountKey=your key",
    TableName = "Events",
    TableEntityMapper = ev => new AuditEventTableEntity(ev)
};
```

Or by using the [fluent configuration API](https://github.com/thepirat000/Audit.NET#configuration-fluent-api) via `UseAzureTableStorage` method:
```c#
Audit.Core.Configuration.Setup()
    .UseAzureTableStorage(_ => _
        .ConnectionString("DefaultEndpointsProtocol=https;AccountName=your account;AccountKey=your key")
        .TableName("Events")
        .EntityMapper(ev => new AuditEventTableEntity(ev)));
```

You can set the entity mapper to return any class implementing [`ITableEntity`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.windowsazure.storage.table.itableentity?view=azure-dotnet), as the provided default [`AuditEventTableEntity`](https://github.com/thepirat000/Audit.NET/tree/master/src/Audit.NET.AzureStorage/ConfigurationApi/AuditEventTableEntity.cs).

As an alternative, you can use `EntityBuilder()` method to configure the columns mapping:
```c#
Audit.Core.Configuration.Setup()
    .UseAzureTableStorage(_ => _
        .ConnectionString("DefaultEndpointsProtocol=https;AccountName=your account;AccountKey=your key")
        .TableName("Events")
        .EntityBuilder(e => e
            .PartitionKey(ev => $"Events{ev.StartDate:yyyyMM}")
            .RowKey(ev => Guid.NewGuid().ToString())
            .Columns(c => c.FromObject(ev => new { Date = ev.StartDate, AuditEventJson = ev.ToJson() }))));
```

#### Provider Options

Mandatory:
- **ConnectionString/ConnectionStringBuilder**: The Azure Storage connection string.

Optional:
- **TableName**: The table name to use. By default, "event" is used as the table name.
- **EntityMapper**: A function that defines a mapping from an Audit Event to a Table Entity. 
- **EntityBuilder**: An alternative way to dynamically specify the table entity mapping for audit events:
  - _PartitionKey_: A function of the audit event that returns the partition key to use.
  - _RowKey_: A function of the audit event that returns the row key to use.
  - _Columns_: Provides a way to generate the extra columns from an anonymous object or a dictionary.

##### Query events

The Azure Table data provider **does not** implements `GetEvent` nor `GetEventAsync` methods.
