# Audit.NET.AzureStorage
**Azure Blob Storage provider for [Audit.NET library](https://github.com/thepirat000/Audit.NET)** (An extensible framework to audit executing operations in .NET).

Store the audit events in an Azure Blob Storage container, in JSON format.

## Install

**[NuGet Package](https://www.nuget.org/packages/Audit.NET.AzureStorage/)**
```
PM> Install-Package Audit.NET.AzureStorage
```

## Usage
Please see the [Audit.NET Readme](https://github.com/thepirat000/Audit.NET#usage)

## Configuration
Set the static `Audit.Core.Configuration.DataProvider` property to set the Azure Storage data provider, or call the `UseAzureBlobStorage()` method on the fluent configuration. This should be done before any `AuditScope` creation, i.e. during application startup.

For example:
```c#
Audit.Core.Configuration.DataProvider = new AzureBlobDataProvider()
{
    ConnectionString = "DefaultEndpointsProtocol=https;AccountName=your account;AccountKey=your key",
    ContainerName = "event",
    BlobNameBuilder = ev => $"{ev.StartDate:yyyy-MM}/{ev.Environment.UserName}/{Guid.NewGuid()}.json"
};
```

Or by using the [fluent configuration API](https://github.com/thepirat000/Audit.NET#configuration-fluent-api):
```c#
Audit.Core.Configuration.Setup()
    .UseAzureBlobStorage(config => config
        .ConnectionString("DefaultEndpointsProtocol=https;AccountName=your account;AccountKey=your key")
        .ContainerName("event")
        .BlobNameBuilder(ev => $"{ev.StartDate:yyyy-MM}/{ev.Environment.UserName}/{Guid.NewGuid()}.json"));
```

### Provider Options

Mandatory:
- **ConnectionString**: The Azure Storage connection string.
- **ContainerName**: The container name to use (see the naming restrictions [here](https://azure.microsoft.com/en-us/documentation/articles/storage-dotnet-how-to-use-blobs/)).

Optional:
- **BlobNameBuilder**: A function that takes an Audit Event and returns a unique blob name for the event. The resulting name can include path information (slash separated sub-folders). By default, a random Guid is used as the blob name.

## Output sample

An example of the output as seen with [Microsoft Azure Storage Explorer](http://storageexplorer.com/):

![AzureStorage sample 1](http://i.imgur.com/XLKluLn.jpg)


