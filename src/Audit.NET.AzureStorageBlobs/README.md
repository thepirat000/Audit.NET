# Audit.NET.AzureStorageBlobs
**Azure Storage Blob** provider for [Audit.NET library](https://github.com/thepirat000/Audit.NET) (An extensible framework to audit executing operations in .NET).

Store the audit events in an Azure Storage Blob container in JSON format.

## Install

**NuGet Package** 
To install the package run the following command on the Package Manager Console:

```
PM> Install-Package Audit.NET.AzureStorageBlobs
```

[![NuGet Status](https://img.shields.io/nuget/v/Audit.NET.AzureStorageBlobs.svg?style=flat)](https://www.nuget.org/packages/Audit.NET.AzureStorageBlobs/)
[![NuGet Count](https://img.shields.io/nuget/dt/Audit.NET.AzureStorageBlobs.svg)](https://www.nuget.org/packages/Audit.NET.AzureStorageBlobs/)

## How it works

This library uses the [Azure.Storage.Blobs](https://docs.microsoft.com/en-us/dotnet/api/overview/azure/storage.blobs-readme) API to store the Audit Events
on Azure Storage Blob containers in JSON format.

## Usage
Please see the [Audit.NET Readme](https://github.com/thepirat000/Audit.NET#usage)


## Configuration
Set the static `Audit.Core.Configuration.DataProvider` property to an instance of `AzureStorageBlobDataProvider`, or call the `UseAzureStorageBlobs()` 
methods on the fluent configuration. This should be done before any `AuditScope` creation, i.e. during application startup.

#### Configuration examples

Using a connection string: 
 
```c#
Audit.Core.Configuration.Setup()
    .UseAzureStorageBlobs(config => config
        .WithConnectionString(Settings.ConnectionString)
        .ContainerName(ev => $"{DateTime.Today:yyyyMMdd}")
        .BlobName(ev => $"{ev.EventType}/{Guid.NewGuid()}.json")
        .AccessTier(AccessTier.Cool)
        .Metadata(ev => new Dictionary<string, string>() { { "user", ev.Environment.UserName } })
        .Tags(ev => new Dictionary<string, string>() { { "eventType", ev.EventType } }));
```

Using a shared key (SharedKeyCredential):

```c#
Audit.Core.Configuration.Setup()
    .UseAzureStorageBlobs(config => config
        .WithCredentials(_ => _
            .Url(Settings.AzureBlobServiceUrl)
            .Credential(new StorageSharedKeyCredential(Settings.AccountName", Settings.AccountKey)))
        .ContainerName(ev => $"{DateTime.Today:yyyyMMdd}")
        .BlobName(ev => $"{ev.EventType}/{Guid.NewGuid()}.json")
        .AccessTier(AccessTier.Cool)
        .Metadata(ev => new Dictionary<string, string>() { { "user", ev.Environment.UserName } }));
```

Using Azure Active Directory (Token Credential):

```c#
var credential = new ClientSecretCredential(
    Settings.TenantId,
    Settings.ApplicationId,
    Settings.ApplicationSecret,
    new TokenCredentialOptions() { AuthorityHost = Settings.AuthEndpoint });

Audit.Core.Configuration.Setup()
    .UseAzureStorageBlobs(config => config
        .WithCredentials(_ => _
            .Url("AzureBlobServiceUrl")
            .Credential(credential))
        .ContainerName("AuditLogs")
        .BlobName(ev => Guid.NewGuid().ToString()));
```

#### Provider Options

##### Authentication options

Depending on the authentication method, you can call one of the following methods:

- **`WithConnectionString`**: Connect using an Azure Storage connection string.
- **`WithServiceUrl`**: Connect using a service URL (anonymous)
- **`WithCredentials`**: Connect using a service URL and credentials (StorageSharedKeyCredential, AzureSasCredential or TokenCredential)

##### Container options

- **`ContainerName`**: The container name to use as a function of the Audit Event (see the naming restrictions [here](https://docs.microsoft.com/en-us/rest/api/storageservices/naming-and-referencing-containers--blobs--and-metadata)). 
- **`BlobName`**: The unique blob name to use as a function of the Audit Event. The resulting name can include path information (slash separated sub-folders). 
- **`AccessTier`**: (optional) The [Access Tier](https://docs.microsoft.com/en-us/azure/storage/blobs/storage-blob-storage-tiers) to use as a function of the Audit Event. 
- **`Metadata`**: (optional) Extra information to include as metadata to be associated with the blob storage resource.
- **`Tags`**: (optional) Extra information to include as tags to be associated with the blob storage resource. 

#### Query events

This provider implements `GetEvent` and `GetEventAsync` methods to obtain an audit event by container name and blob name:

```c#
var event = blobDataProvider.GetEvent("containerName", "blobName");
```

## ZZZ Projects - Sponsorship

[Entity Framework Extensions](https://entityframework-extensions.net/) and [Dapper Plus](https://dapper-plus.net/) are major sponsors and are proud to contribute to the development of Audit.NET

Combine the power of auditing with the speed of Bulk Operations to get the best of both worlds — audit and performance.

[![Entity Framework Extensions - Sponsor](https://raw.githubusercontent.com/thepirat000/Audit.NET/master/documents/entity-framework-extensions-sponsor.png)](https://entityframework-extensions.net/bulk-insert)

[![Dapper Plus - Sponsor](https://raw.githubusercontent.com/thepirat000/Audit.NET/master/documents/dapper-plus-sponsor.png)](https://dapper-plus.net/bulk-insert)