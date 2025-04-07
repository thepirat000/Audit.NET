# Audit.NET.AzureStorageTables
**Azure Storage Tables** provider for [Audit.NET library](https://github.com/thepirat000/Audit.NET) (An extensible framework to audit executing operations in .NET).

Store the audit events in an Azure Table Storage.

## Install

**NuGet Package** 
To install the package run the following command on the Package Manager Console:

```
PM> Install-Package Audit.NET.AzureStorageTables
```

[![NuGet Status](https://img.shields.io/nuget/v/Audit.NET.AzureStorageTables.svg?style=flat)](https://www.nuget.org/packages/Audit.NET.AzureStorageTables/)
[![NuGet Count](https://img.shields.io/nuget/dt/Audit.NET.AzureStorageTables.svg)](https://www.nuget.org/packages/Audit.NET.AzureStorageTables/)

## How it works

This library uses the [Azure.Data.Tables](https://docs.microsoft.com/en-us/dotnet/api/overview/azure/storage.blobs-readme) API to store the Audit Events
on Azure Table Storage. Please see the [Audit.NET Readme](https://github.com/thepirat000/Audit.NET#usage).

## Usage

Set the static `Audit.Core.Configuration.DataProvider` property to an instance of `AzureTableDataProvider`, or call the `UseAzureTableStorage()` 
method on the fluent configuration. This should be done before any `AuditScope` creation, i.e. during application startup.

The `AzureTableDataProvider` constructor accepts a `Action<AzureTableDataProviderConfigurator>` parameter to configure the provider.

For Example:

```c#
Audit.Core.Configuration.DataProvider = new AzureTableDataProvider(config => config...)
```

Or: 

```c#
Audit.Core.Configuration.Setup()
    .UseAzureTableStorage(config => config...)
```

## Configuration

### Connection options

The Azure Storage connection can be configured in three different ways:

- **`ConnectionString(string)`**: Specifies the connection string to connect to the Azure Table Storage. 
- **`Endpoint(Uri, Credentials)`**: Specifies the endpoint and optionally the credentials to use.
- **`TableClientFactory(Func<AuditEvent, TableClient>)`**: Specifies a table client factory that returns the `TableClient` to use for a given Audit Event.

### Table options

- **`TableName(string)`**: Specifies the table name to use. Defaults to `audit`.
- **`TableName(Func<AuditEvent, string>)`**: Specifies a function that returns the table name to use for a given Audit Event.
- **`ClientOptions(TableClientOptions)`**: Specifies the Table Client Options to use when connecting to the Azure Table Storage.
- **`EntityMapper(Func<AuditEvent, ITableEntity>)`**: Specifies how to map the AuditEvent to an Azure TableEntity object. By default, an instance of `AuditEventTableEntity` is used,
which adds one column named "AuditEvent" containing the Audit Event JSON representation.
- **`EntityBuilder(Action<IAzureTableRowConfigurator>)`**: Specifies how to dynamically create a Table Entity from the Audit Event. Use this method as an alternative to `EntityMapper()` to build the columns dynamically (see next section).

### Entity Builder options 

- **`PartitionKey(string)`**: Specifies the partition key to use. Defaults to the name of the Audit Event type.
- **`PartitionKey(Func<AuditEvent, string>)`**: Sets the partition key to use as a function of the Audit Event
- **`RowKey(string)`**: Sets the row key to use as a function of the Audit Event. Default is a random Guid.
- **`Columns(Action<IAzureTableColumnsConfigurator>)`**: Defines a configuration for the extra columns (properties) on the entity. 
Default is one column "AuditEvent" with the audit event JSON. (see next section).

### Columns Builder options

- **`FromDictionary(Func<AuditEvent, IDictionary<string, object>>)`**: Sets the columns (properties) values from a dictionary of strings and objects. 
Key is the column name, Value is the column value. (Value must be of a simple type or convertible to string).

- **`FromObject(Func<AuditEvent, object>)`**: Sets the columns (properties) values from an object or an anonymous object. 
The object properties Values must be of a simple type or convertible to string.

## Configuration examples

- Providing the Connection String and the Table name. 
Using the default entity mapping.

    ```c#
    Audit.Core.Configuration.Setup()
        .UseAzureTableStorage(config => config
            .ConnectionString(Settings.ConnectionString)
            .TableName(Settings.TableName));
    ```

- Providing the Endpoint with a shared key credential, using the Audit Event's EventType as the Table name..
Using the default entity mapping.

    ```c#
    Audit.Core.Configuration.Setup()
        .UseAzureTableStorage(config => config
            .Endpoint(new Uri(Settings.TableEndpointUrl), new TableSharedKeyCredential(Settings.AccountName, Settings.AccountKey))
            .TableName(auditEvent => auditEvent.EventType);
    ```

- Providing a custom TableClient factory. 
Using the default entity mapping.

    ```c#
    Audit.Core.Configuration.Setup()
        .UseAzureTableStorage(config => config
            .TableClientFactory(auditEvent => GetTableClient(auditEvent.EventType)));
    ```

- A more complex scenario providing the Endpoint, Table name and custom Client Options. 
Configuring a custom entity mapping that uses the Event Type as the partition key, 
a random Guid as the Row Key and includes 4 extra columns.

    ```c#
    Audit.Core.Configuration.Setup()
        .UseAzureTableStorage(config => config
            .Endpoint(new Uri(Settings.TableEndpointUrl))
            .TableName(Settings.TableName)
            .ClientOptions(new TableClientOptions() { Retry = { MaxRetries = 66 } })
            .EntityBuilder(builder => builder
                .PartitionKey(auditEvent => auditEvent.EventType)
                .RowKey(auditEvent => Guid.NewGuid().ToString("N"))
                .Columns(col => col
                    .FromDictionary(auditEvent => new Dictionary<string, object>()
                    {
                        { "EventType", auditEvent.EventType },
                        { "UserName", auditEvent.Environment.UserName },
                        { "EventDuration", auditEvent.Duration },
                        { "Data", auditEvent.ToJson() }
                    }))));
    ```

## ZZZ Projects - Sponsorship

[Entity Framework Extensions](https://entityframework-extensions.net/) and [Dapper Plus](https://dapper-plus.net/) are major sponsors and are proud to contribute to the development of Audit.NET

Combine the power of auditing with the speed of Bulk Operations to get the best of both worlds — audit and performance.

<a href="https://entityframework-extensions.net/bulk-insert"><img width="728" height="90" alt="Entity Framework Extensions - Sponsor" src="https://raw.githubusercontent.com/JonathanMagnan/Audit.NET/master/entity-framework-extensions-sponsor.png" /></a>

<a href="https://dapper-plus.net/bulk-insert"><img width="728" height="90" alt="Dapper Plus - Sponsor" src="https://raw.githubusercontent.com/JonathanMagnan/Audit.NET/master/dapper-plus-sponsor.png" /></a>



